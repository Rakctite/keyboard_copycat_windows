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
    public void ProgramLogsBleWriteStartAndCompletion()
    {
        var programSource = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Program.cs"));
        var clientSource = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Ble",
            "BleKeyboardBridgeClient.cs"));

        Assert.Contains("[ble] write start", programSource);
        Assert.Contains("[ble] write done", clientSource);
        Assert.Contains("FormatReport", programSource);
    }
}
