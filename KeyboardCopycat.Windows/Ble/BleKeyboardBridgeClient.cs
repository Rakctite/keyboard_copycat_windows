using KeyboardCopycat.Windows.Input;
using System.Runtime.InteropServices;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace KeyboardCopycat.Windows.Ble;

public sealed class BleKeyboardBridgeClient : IAsyncDisposable
{
    private readonly BleBridgeOptions options;
    private BluetoothLEDevice? device;
    private GattDeviceService? reportService;
    private GattSession? gattSession;
    private GattCharacteristic? reportCharacteristic;

    public BleKeyboardBridgeClient(BleBridgeOptions options)
    {
        this.options = options;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Scanning BLE advertisements...");
        var advertisedDevice = await StartAdvertisementScanAsync(cancellationToken);
        if (advertisedDevice is not null)
        {
            Console.WriteLine(
                $"[ble] opening advertised device address=0x{advertisedDevice.Value.Address:X12} type={advertisedDevice.Value.AddressType}");
            device = await BluetoothLEDevice.FromBluetoothAddressAsync(
                advertisedDevice.Value.Address,
                advertisedDevice.Value.AddressType);
        }

        if (device is null)
        {
            Console.WriteLine("Advertisement scan did not find the bridge; checking known BLE devices...");
            device = await OpenKnownDeviceAsync(cancellationToken);
        }

        if (device is null)
        {
            throw new InvalidOperationException(
                $"BLE device '{options.DeviceName}' was not found. Make sure the Arduino firmware is powered and advertising.");
        }

        reportService = await OpenGattServiceBySelectorAsync(cancellationToken);
        if (reportService is null)
        {
            await OpenReportCharacteristicAsync(device);
        }
        else
        {
            await OpenReportCharacteristicFromServiceAsync(reportService);
        }
    }

    private async Task<BluetoothLEDevice?> OpenKnownDeviceAsync(CancellationToken cancellationToken)
    {
        var selector = BluetoothLEDevice.GetDeviceSelector();
        var devices = await DeviceInformation.FindAllAsync(selector);
        var match = devices.FirstOrDefault(d =>
            string.Equals(d.Name, options.DeviceName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await BluetoothLEDevice.FromIdAsync(match.Id);
    }

    private async Task<AdvertisedBleDevice?> StartAdvertisementScanAsync(CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<AdvertisedBleDevice?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active,
        };

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeout.Token);
        using var registration = linked.Token.Register(() => completion.TrySetResult(null));

        watcher.Received += (_, args) =>
        {
            var advertisement = args.Advertisement;
            var hasMatchingName = string.Equals(
                advertisement.LocalName,
                options.DeviceName,
                StringComparison.OrdinalIgnoreCase);
            var hasMatchingService = advertisement.ServiceUuids.Contains(options.ServiceUuid);

            if (hasMatchingName || hasMatchingService)
            {
                completion.TrySetResult(new AdvertisedBleDevice(
                    args.BluetoothAddress,
                    args.BluetoothAddressType));
            }
        };

        watcher.Start();
        try
        {
            return await completion.Task;
        }
        finally
        {
            watcher.Stop();
        }
    }

    private readonly record struct AdvertisedBleDevice(
        ulong Address,
        BluetoothAddressType AddressType);

    private async Task OpenReportCharacteristicAsync(BluetoothLEDevice connectedDevice)
    {
        var serviceResult = await GetGattServicesWithRetryAsync(connectedDevice);
        if (serviceResult.Status != GattCommunicationStatus.Success ||
            serviceResult.Services.Count == 0)
        {
            throw new InvalidOperationException(
                $"BLE service {options.ServiceUuid} was not found on '{options.DeviceName}'.");
        }

        reportService = serviceResult.Services[0];
        await OpenReportCharacteristicFromServiceAsync(reportService);
    }

    private async Task<GattDeviceService?> OpenGattServiceBySelectorAsync(
        CancellationToken cancellationToken)
    {
        var selector = GattDeviceService.GetDeviceSelectorFromUuid(options.ServiceUuid);
        Console.WriteLine("[ble] discovering service by GATT selector");
        var services = await DeviceInformation.FindAllAsync(selector);
        Console.WriteLine($"[ble] GATT selector candidates={services.Count}");

        foreach (var serviceInfo in services)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"[ble] service candidate name='{serviceInfo.Name}' id='{serviceInfo.Id}'");
            var service = await GattDeviceService.FromIdAsync(serviceInfo.Id);
            if (service is not null)
            {
                Console.WriteLine("[ble] opened service by GATT selector");
                return service;
            }
        }

        Console.WriteLine("[ble] GATT selector did not open a service; falling back to device service discovery");
        return null;
    }

    private async Task OpenReportCharacteristicFromServiceAsync(GattDeviceService service)
    {
        gattSession = await GattSession.FromDeviceIdAsync(service.Session.DeviceId);
        gattSession.MaintainConnection = true;

        var characteristicResult = await GetCharacteristicsWithRetryAsync(service);
        if (characteristicResult.Status != GattCommunicationStatus.Success ||
            characteristicResult.Characteristics.Count == 0)
        {
            throw new InvalidOperationException(
                $"BLE report characteristic {options.ReportCharacteristicUuid} was not found.");
        }

        reportCharacteristic = characteristicResult.Characteristics[0];
        Console.WriteLine($"[ble] characteristic properties={reportCharacteristic.CharacteristicProperties}");
    }

    private async Task<GattDeviceServicesResult> GetGattServicesWithRetryAsync(
        BluetoothLEDevice connectedDevice)
    {
        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Console.WriteLine($"[ble] discovering service attempt={attempt}");
                return await connectedDevice.GetGattServicesForUuidAsync(
                    options.ServiceUuid,
                    BluetoothCacheMode.Uncached);
            }
            catch (COMException ex)
            {
                Console.WriteLine(
                    $"[ble] service discovery COMException attempt={attempt} HResult=0x{ex.HResult:X8} message={ex.Message}");
                if (attempt < maxAttempts)
                {
                    await Task.Delay(500);
                    continue;
                }
                throw;
            }
        }

        throw new InvalidOperationException("BLE service discovery retry loop exited unexpectedly.");
    }

    private async Task<GattCharacteristicsResult> GetCharacteristicsWithRetryAsync(
        GattDeviceService service)
    {
        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Console.WriteLine($"[ble] discovering characteristic attempt={attempt}");
                return await service.GetCharacteristicsForUuidAsync(
                    options.ReportCharacteristicUuid,
                    BluetoothCacheMode.Uncached);
            }
            catch (COMException ex)
            {
                Console.WriteLine(
                    $"[ble] characteristic discovery COMException attempt={attempt} HResult=0x{ex.HResult:X8} message={ex.Message}");
                if (attempt < maxAttempts)
                {
                    await Task.Delay(500);
                    continue;
                }
                throw;
            }
        }

        throw new InvalidOperationException("BLE characteristic discovery retry loop exited unexpectedly.");
    }

    public void SendReport(HidReport report)
    {
        if (reportCharacteristic is null)
        {
            throw new InvalidOperationException("BLE bridge is not connected.");
        }

        var formatted = ReportFormatter.FormatReport(report);
        using var writer = new DataWriter();
        writer.WriteBytes(report.ToArray());
        var operation = reportCharacteristic.WriteValueAsync(
            writer.DetachBuffer(),
            GattWriteOption.WriteWithResponse);
        operation.Completed = (op, asyncStatus) =>
        {
            if (asyncStatus == AsyncStatus.Completed)
            {
                var status = op.GetResults();
                Console.WriteLine($"[ble] write done status={status} report={formatted}");
            }
            else
            {
                Console.WriteLine($"[ble] write {asyncStatus} report={formatted}");
            }
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (reportCharacteristic is not null)
        {
            var release = new HidReport(new byte[HidReport.Size]);
            try
            {
                SendReport(release);
                await Task.Delay(100);
            }
            catch
            {
                // Best effort release on shutdown.
            }
        }

        device?.Dispose();
        reportService?.Dispose();
        gattSession?.Dispose();
    }
}
