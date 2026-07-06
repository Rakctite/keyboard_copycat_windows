namespace KeyboardCopycat.Windows.Tests;

public sealed class BleServerSourceTests
{
    [Fact]
    public void WindowsAppPublishesKeyboardReportsAsGattNotifications()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Ble",
            "BleKeyboardReportServer.cs"));

        Assert.Contains("GattServiceProvider.CreateAsync", source);
        Assert.Contains("GattLocalCharacteristicParameters", source);
        Assert.Contains("GattCharacteristicProperties.Notify", source);
        Assert.Contains("NotifyValueAsync", source);
        Assert.Contains("StartAdvertising", source);
    }

    [Fact]
    public void ProgramStartsBleServerInsteadOfConnectingToArduinoGatt()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Program.cs"));

        Assert.Contains("BleKeyboardReportServer", source);
        Assert.Contains("StartAsync", source);
        Assert.Contains("PublishReportAsync", source);
        Assert.DoesNotContain("BleKeyboardBridgeClient", source);
    }
}
