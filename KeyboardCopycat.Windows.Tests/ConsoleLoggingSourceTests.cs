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
    public void SendQueueLogsSuccessfulBleWrites()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Ble",
            "KeyboardReportSendQueue.cs"));

        Assert.Contains("[ble] wrote report", source);
        Assert.Contains("FormatReport", source);
    }
}
