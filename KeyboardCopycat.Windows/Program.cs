using KeyboardCopycat.Windows.Ble;
using KeyboardCopycat.Windows.Input;
using KeyboardCopycat.Windows.Win32;

namespace KeyboardCopycat.Windows;

internal static class Program
{
    private static async Task<int> Main()
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, args) =>
        {
            args.Cancel = true;
            cancellation.Cancel();
        };

        var builder = new HidReportBuilder();
        await using var bleClient = new BleKeyboardBridgeClient(BleBridgeOptions.Defaults);

        Console.WriteLine("Searching for KeyboardBridge...");
        await bleClient.ConnectAsync(cancellation.Token);
        Console.WriteLine("Connected. Forwarding keyboard input. Press Ctrl+C to stop.");

        using var hook = new LowLevelKeyboardHook();
        using var sendQueue = new KeyboardReportSendQueue(bleClient, cancellation.Token);
        byte[]? lastSentReport = null;

        hook.KeyChanged += (_, args) =>
        {
            var report = args.IsDown
                ? builder.KeyDown(args.VirtualKeyCode)
                : builder.KeyUp(args.VirtualKeyCode);
            var reportBytes = report.ToArray();
            if (ReportsEqual(lastSentReport, reportBytes))
            {
                return;
            }

            lastSentReport = reportBytes;
            Console.WriteLine(
                $"[input] {(args.IsDown ? "down" : "up")} vk=0x{args.VirtualKeyCode:X2} report={ReportFormatter.FormatReport(report)}");
            sendQueue.Enqueue(report);
        };

        hook.Start();
        Console.WriteLine("Keyboard hook installed. Waiting for key events...");

        Win32MessageLoop.RunUntilCancelled(cancellation.Token);
        await bleClient.SendReportAsync(builder.ReleaseAll(), CancellationToken.None);

        return 0;
    }

    private static bool ReportsEqual(byte[]? left, byte[] right)
    {
        return left is not null && left.SequenceEqual(right);
    }
}
