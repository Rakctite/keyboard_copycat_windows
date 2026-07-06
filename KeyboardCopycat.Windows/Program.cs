using KeyboardCopycat.Windows.Ble;
using KeyboardCopycat.Windows.Input;
using KeyboardCopycat.Windows.Win32;

namespace KeyboardCopycat.Windows;

internal static class Program
{
    private static readonly TimeSpan HeldKeyHeartbeatInterval = TimeSpan.FromMilliseconds(1000);

    private static async Task<int> Main()
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, args) =>
        {
            args.Cancel = true;
            cancellation.Cancel();
        };

        var builder = new HidReportBuilder();
        await using var bleServer = new BleKeyboardReportServer(BleBridgeOptions.Defaults);

        Console.WriteLine("Starting KeyboardBridge GATT server...");
        await bleServer.StartAsync();
        Console.WriteLine("Advertising. Forwarding keyboard input to subscribed Arduino clients. Press Ctrl+C to stop.");

        using var hook = new LowLevelKeyboardHook();
        object reportLock = new();
        byte[]? lastSentReport = null;
        HidReport? latestHeldReport = null;

        var heartbeatTask = RunHeldKeyHeartbeatAsync(
            bleServer,
            () =>
            {
                lock (reportLock)
                {
                    return latestHeldReport;
                }
            },
            cancellation.Token);

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
            lock (reportLock)
            {
                latestHeldReport = IsReleaseReport(report) ? null : report;
            }

            Console.WriteLine(
                $"[input] {(args.IsDown ? "down" : "up")} vk=0x{args.VirtualKeyCode:X2} report={ReportFormatter.FormatReport(report)}");
            _ = bleServer.PublishReportAsync(report);
        };

        hook.Start();
        Console.WriteLine("Keyboard hook installed. Waiting for key events...");

        Win32MessageLoop.RunUntilCancelled(cancellation.Token);
        await bleServer.PublishReportAsync(builder.ReleaseAll());
        await heartbeatTask;

        return 0;
    }

    private static async Task RunHeldKeyHeartbeatAsync(
        BleKeyboardReportServer bleServer,
        Func<HidReport?> getLatestHeldReport,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(HeldKeyHeartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var report = getLatestHeldReport();
                if (report.HasValue)
                {
                    await bleServer.PublishReportAsync(report.Value);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static bool ReportsEqual(byte[]? left, byte[] right)
    {
        return left is not null && left.SequenceEqual(right);
    }

    private static bool IsReleaseReport(HidReport report)
    {
        return report.ToArray().All(value => value == 0);
    }
}
