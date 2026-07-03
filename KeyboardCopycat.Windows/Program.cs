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

        hook.KeyChanged += (_, args) =>
        {
            var report = args.IsDown
                ? builder.KeyDown(args.VirtualKeyCode)
                : builder.KeyUp(args.VirtualKeyCode);
            Console.WriteLine(
                $"[input] {(args.IsDown ? "down" : "up")} vk=0x{args.VirtualKeyCode:X2} report={ReportFormatter.FormatReport(report)}");
            sendQueue.Enqueue(report);
        };

        hook.Start();

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            await bleClient.SendReportAsync(builder.ReleaseAll(), CancellationToken.None);
        }

        return 0;
    }
}
