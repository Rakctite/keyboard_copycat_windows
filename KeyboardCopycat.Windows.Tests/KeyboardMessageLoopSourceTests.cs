namespace KeyboardCopycat.Windows.Tests;

public sealed class KeyboardMessageLoopSourceTests
{
    [Fact]
    public void ProgramRunsWin32MessageLoopAfterInstallingKeyboardHook()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Program.cs"));

        Assert.Contains("Win32MessageLoop.RunUntilCancelled", source);
        Assert.DoesNotContain("Task.Delay(Timeout.InfiniteTimeSpan", source);
    }

    [Fact]
    public void Win32MessageLoopPumpsMessagesAndPostsQuitOnCancellation()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "KeyboardCopycat.Windows",
            "Win32",
            "Win32MessageLoop.cs"));

        Assert.Contains("GetMessage", source);
        Assert.Contains("TranslateMessage", source);
        Assert.Contains("DispatchMessage", source);
        Assert.Contains("PostThreadMessage", source);
        Assert.Contains("WM_QUIT", source);
    }
}
