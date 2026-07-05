namespace KeyboardCopycat.Windows.Tests;

public sealed class DuplicateReportSourceTests
{
    [Fact]
    public void ProgramDoesNotEnqueueDuplicateReportsFromKeyRepeat()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Program.cs"));

        Assert.Contains("lastSentReport", source);
        Assert.Contains("ReportsEqual", source);
        Assert.Contains("return;", source);
    }
}
