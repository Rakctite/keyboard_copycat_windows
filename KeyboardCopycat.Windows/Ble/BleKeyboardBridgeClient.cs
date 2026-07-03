using KeyboardCopycat.Windows.Input;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace KeyboardCopycat.Windows.Ble;

public sealed class BleKeyboardBridgeClient : IAsyncDisposable
{
    private readonly BleBridgeOptions options;
    private BluetoothLEDevice? device;
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

        var characteristicResult = await serviceResult.Services[0].GetCharacteristicsForUuidAsync(
            options.ReportCharacteristicUuid,
            BluetoothCacheMode.Uncached);
        if (characteristicResult.Status != GattCommunicationStatus.Success ||
            characteristicResult.Characteristics.Count == 0)
        {
            throw new InvalidOperationException(
                $"BLE report characteristic {options.ReportCharacteristicUuid} was not found.");
        }

        reportCharacteristic = characteristicResult.Characteristics[0];
    }

    public async Task SendReportAsync(HidReport report, CancellationToken cancellationToken)
    {
        if (reportCharacteristic is null)
        {
            throw new InvalidOperationException("BLE bridge is not connected.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var writer = new DataWriter();
        writer.WriteBytes(report.ToArray());
        var status = await reportCharacteristic.WriteValueAsync(
            writer.DetachBuffer(),
            GattWriteOption.WriteWithoutResponse);

        if (status != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException($"BLE report write failed with status {status}.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (reportCharacteristic is not null)
        {
            var release = new HidReport(new byte[HidReport.Size]);
            try
            {
                await SendReportAsync(release, CancellationToken.None);
            }
            catch
            {
                // Best effort release on shutdown.
            }
        }

        device?.Dispose();
    }
}
