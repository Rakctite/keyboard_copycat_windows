namespace KeyboardCopycat.Windows.Tests;

public sealed class ConsoleLoggingSourceTests
{
    [Fact]
    public void ProgramLogsHookedKeyReportsBeforeSending()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Program.cs"));

        Assert.Contains("FormatReport", source);
        Assert.Contains("[input]", source);
        Assert.Contains("vk=0x", source);
    }

    [Fact]
    public void ServerLogsBleNotifications()
    {
        var programSource = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Program.cs"));
        var serverSource = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Ble",
            "BleKeyboardReportServer.cs"));

        Assert.Contains("PublishReportAsync", programSource);
        Assert.Contains("[ble] notify status=", serverSource);
        Assert.Contains("FormatReport", programSource);
    }
}
