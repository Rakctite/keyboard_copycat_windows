namespace KeyboardCopycat.Windows.Tests;

public sealed class WindowsControlPanelSourceTests
{
    [Fact]
    public void ProjectUsesWindowsFormsControlPanel()
    {
        var project = Read("KeyboardCopycat.Windows/KeyboardCopycat.Windows.csproj");
        var program = Read("KeyboardCopycat.Windows/Program.cs");
        var form = Read("KeyboardCopycat.Windows/Ui/MainForm.cs");

        Assert.Contains("<UseWindowsForms>true</UseWindowsForms>", project);
        Assert.Contains("Application.Run", program);
        Assert.Contains("MainForm", program);
        Assert.Contains("TextBox", form);
        Assert.Contains("ReadOnly = true", form);
        Assert.Contains("LockInputRequested", form);
        Assert.Contains("UnlockInputRequested", form);
        Assert.Contains("SetArduinoConnected", form);
    }

    [Fact]
    public void KeyboardHookCanSuppressLocalWindowsInput()
    {
        var hook = Read("KeyboardCopycat.Windows/Win32/LowLevelKeyboardHook.cs");

        Assert.Contains("SuppressKeyboardInput", hook);
        Assert.Contains("return 1", hook);
    }

    [Fact]
    public void BleServerPublishesArduinoSubscriptionState()
    {
        var server = Read("KeyboardCopycat.Windows/Ble/BleKeyboardReportServer.cs");

        Assert.Contains("ArduinoConnectionChanged", server);
        Assert.Contains("SubscribedClientsChanged", server);
        Assert.Contains("SubscribedClients.Count", server);
    }

    private static string Read(string path)
    {
        return File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            path));
    }
}
