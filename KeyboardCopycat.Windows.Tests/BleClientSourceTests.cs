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
}
