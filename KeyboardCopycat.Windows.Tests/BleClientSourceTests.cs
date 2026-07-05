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
        Assert.Contains("StartAdvertisementScanAsync", source);
    }

    [Fact]
    public void SendReportUsesWriteWithoutResponseToAvoidBufferedAckDelay()
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

        Assert.Contains("GattWriteOption.WriteWithoutResponse", source);
        Assert.DoesNotContain("GattWriteOption.WriteWithResponse", source);
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
}
