namespace KeyboardCopycat.Windows.Tests;

public sealed class BleClientSourceTests
{
    [Fact]
    public void ConnectAsyncScansBleAdvertisementsBeforeOpeningGattDevice()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Ble",
            "BleKeyboardBridgeClient.cs"));

        Assert.Contains("BluetoothLEAdvertisementWatcher", source);
        Assert.Contains("BluetoothLEDevice.FromBluetoothAddressAsync", source);
        Assert.Contains("BluetoothAddressType", source);
        Assert.Contains("StartAdvertisementScanAsync", source);
    }

    [Fact]
    public void SendReportUsesWriteWithResponseForWriteOnlyCharacteristic()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Ble",
            "BleKeyboardBridgeClient.cs"));

        Assert.Contains("GattWriteOption.WriteWithResponse", source);
        Assert.DoesNotContain("GattWriteOption.WriteWithoutResponse", source);
    }

    [Fact]
    public void ClientKeepsGattServiceAndSessionAliveAndLogsProperties()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Ble",
            "BleKeyboardBridgeClient.cs"));

        Assert.Contains("private GattDeviceService? reportService;", source);
        Assert.Contains("private GattSession? gattSession;", source);
        Assert.Contains("MaintainConnection = true", source);
        Assert.Contains("CharacteristicProperties", source);
    }

    [Fact]
    public void ClientRetriesGattDiscoveryAndLogsComExceptionHResult()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Ble",
            "BleKeyboardBridgeClient.cs"));

        Assert.Contains("COMException", source);
        Assert.Contains("HResult", source);
        Assert.Contains("GetGattServicesWithRetryAsync", source);
        Assert.Contains("Task.Delay", source);
    }

    [Fact]
    public void ClientHandlesFinalGattDiscoveryFailureAndFallsBackToCachedDiscovery()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Ble",
            "BleKeyboardBridgeClient.cs"));

        Assert.Contains("GetGattServicesWithCacheFallbackAsync", source);
        Assert.Contains("BluetoothCacheMode.Cached", source);
        Assert.Contains("service discovery failed after retries", source);
        Assert.DoesNotContain("return await connectedDevice.GetGattServicesForUuidAsync(\r\n            options.ServiceUuid,\r\n            BluetoothCacheMode.Uncached);", source);
    }
}
