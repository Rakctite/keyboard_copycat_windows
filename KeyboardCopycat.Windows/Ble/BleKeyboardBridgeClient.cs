using KeyboardCopycat.Windows.Input;
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
        var advertisedAddress = await StartAdvertisementScanAsync(cancellationToken);
        if (advertisedAddress.HasValue)
        {
            device = await BluetoothLEDevice.FromBluetoothAddressAsync(advertisedAddress.Value);
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

        await OpenReportCharacteristicAsync(device);
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

    private async Task<ulong?> StartAdvertisementScanAsync(CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<ulong?>(
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
                completion.TrySetResult(args.BluetoothAddress);
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

    private async Task OpenReportCharacteristicAsync(BluetoothLEDevice connectedDevice)
    {
        var serviceResult = await connectedDevice.GetGattServicesForUuidAsync(
            options.ServiceUuid,
            BluetoothCacheMode.Uncached);
        if (serviceResult.Status != GattCommunicationStatus.Success ||
            serviceResult.Services.Count == 0)
        {
            throw new InvalidOperationException(
                $"BLE service {options.ServiceUuid} was not found on '{options.DeviceName}'.");
        }

        reportService = serviceResult.Services[0];
        gattSession = await GattSession.FromDeviceIdAsync(reportService.Session.DeviceId);
        gattSession.MaintainConnection = true;

        var characteristicResult = await reportService.GetCharacteristicsForUuidAsync(
            options.ReportCharacteristicUuid,
            BluetoothCacheMode.Uncached);
        if (characteristicResult.Status != GattCommunicationStatus.Success ||
            characteristicResult.Characteristics.Count == 0)
        {
            throw new InvalidOperationException(
                $"BLE report characteristic {options.ReportCharacteristicUuid} was not found.");
        }

        reportCharacteristic = characteristicResult.Characteristics[0];
        Console.WriteLine($"[ble] characteristic properties={reportCharacteristic.CharacteristicProperties}");
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
            GattWriteOption.WriteWithoutResponse);
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
