using KeyboardCopycat.Windows.Input;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace KeyboardCopycat.Windows.Ble;

public sealed class BleKeyboardReportServer : IAsyncDisposable
{
    private readonly BleBridgeOptions options;
    private readonly SemaphoreSlim notifyLock = new(1, 1);
    private GattServiceProvider? provider;
    private GattLocalCharacteristic? reportCharacteristic;

    public BleKeyboardReportServer(BleBridgeOptions options)
    {
        this.options = options;
    }

    public async Task StartAsync()
    {
        var result = await GattServiceProvider.CreateAsync(options.ServiceUuid);
        if (result.Error != BluetoothError.Success)
        {
            throw new InvalidOperationException($"Failed to create GATT service provider: {result.Error}");
        }

        provider = result.ServiceProvider;

        var parameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Notify | GattCharacteristicProperties.Read,
            ReadProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "Keyboard report",
        };

        var characteristicResult = await provider.Service.CreateCharacteristicAsync(
            options.ReportCharacteristicUuid,
            parameters);
        if (characteristicResult.Error != BluetoothError.Success)
        {
            throw new InvalidOperationException($"Failed to create report characteristic: {characteristicResult.Error}");
        }

        reportCharacteristic = characteristicResult.Characteristic;
        reportCharacteristic.ReadRequested += OnReadRequested;

        provider.StartAdvertising(new GattServiceProviderAdvertisingParameters
        {
            IsConnectable = true,
            IsDiscoverable = true,
        });

        Console.WriteLine($"[ble] advertising service={options.ServiceUuid}");
        Console.WriteLine($"[ble] report characteristic={options.ReportCharacteristicUuid}");
    }

    public async Task PublishReportAsync(HidReport report)
    {
        if (reportCharacteristic is null)
        {
            throw new InvalidOperationException("BLE report server is not started.");
        }

        await notifyLock.WaitAsync();
        try
        {
            using var writer = new DataWriter();
            writer.WriteBytes(report.ToArray());
            var status = await reportCharacteristic.NotifyValueAsync(writer.DetachBuffer());
            Console.WriteLine($"[ble] notify status={status} report={ReportFormatter.FormatReport(report)}");
        }
        finally
        {
            notifyLock.Release();
        }
    }

    private static async void OnReadRequested(
        GattLocalCharacteristic sender,
        GattReadRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            var request = await args.GetRequestAsync();
            if (request is null)
            {
                return;
            }

            using var writer = new DataWriter();
            writer.WriteBytes(new byte[HidReport.Size]);
            request.RespondWithValue(writer.DetachBuffer());
        }
        finally
        {
            deferral.Complete();
        }
    }

    public ValueTask DisposeAsync()
    {
        provider?.StopAdvertising();
        notifyLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
