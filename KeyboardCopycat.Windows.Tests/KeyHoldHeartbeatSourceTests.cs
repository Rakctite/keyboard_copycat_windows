namespace KeyboardCopycat.Windows.Tests;

public sealed class KeyHoldHeartbeatSourceTests
{
    [Fact]
    public void ProgramKeepsHeldKeysAliveWithPeriodicBleHeartbeat()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Program.cs"));

        Assert.Contains("PeriodicTimer", source);
        Assert.Contains("HeldKeyHeartbeatInterval", source);
        Assert.Contains("latestHeldReport", source);
        Assert.Contains("IsReleaseReport", source);
        Assert.Contains("PublishReportAsync(report)", source);
    }
}
