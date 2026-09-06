using KeyboardCopycat.Windows.Ble;
using KeyboardCopycat.Windows.Input;
using KeyboardCopycat.Windows.Ui;
using KeyboardCopycat.Windows.Win32;

namespace KeyboardCopycat.Windows;

internal static class Program
{
    private static readonly TimeSpan HeldKeyHeartbeatInterval = TimeSpan.FromMilliseconds(1000);

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        DiagnosticsLog.Start();

        using var cancellation = new CancellationTokenSource();
        var form = new MainForm();
        using var logWriter = new UiLogTextWriter(form.AppendLog);
        Console.SetOut(logWriter);
        Console.SetError(logWriter);

        Task? runtimeTask = null;
        form.Shown += (_, _) =>
        {
            runtimeTask = RunBridgeAsync(form, cancellation.Token);
            _ = runtimeTask.ContinueWith(
                task => DiagnosticsLog.Write($"[fatal] {task.Exception?.GetBaseException()}"),
                TaskContinuationOptions.OnlyOnFaulted);
        };
        form.FormClosing += (_, _) => cancellation.Cancel();

        Application.Run(form);

        try
        {
            runtimeTask?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task RunBridgeAsync(MainForm form, CancellationToken cancellationToken)
    {
        var builder = new HidReportBuilder();
        await using var bleServer = new BleKeyboardReportServer(BleBridgeOptions.Defaults);

        bleServer.ArduinoConnectionChanged += (_, connected) => form.SetArduinoConnected(connected);

        Console.WriteLine("Starting KeyboardBridge GATT server...");
        await bleServer.StartAsync();
        form.SetArduinoConnected(bleServer.IsArduinoConnected);
        Console.WriteLine("Advertising. Forwarding keyboard input to subscribed Arduino clients.");

        using var hook = new LowLevelKeyboardHook();
        var routing = new InputRoutingController();
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
            cancellationToken);

        form.LockInputRequested += (_, _) =>
        {
            if (routing.SetMode(InputRoutingMode.ArduinoOnly))
            {
                ApplyRoutingMode(InputRoutingMode.ArduinoOnly);
            }
        };
        form.UnlockInputRequested += (_, _) =>
        {
            if (routing.SetMode(InputRoutingMode.HostOnly))
            {
                ApplyRoutingMode(InputRoutingMode.HostOnly);
            }
        };

        hook.KeyChanged += (_, args) =>
        {
            var changedMode = routing.HandleKey(args.VirtualKeyCode, args.IsDown);
            if (changedMode.HasValue)
            {
                ApplyRoutingMode(changedMode.Value);
                return;
            }

            if (routing.Mode == InputRoutingMode.HostOnly)
            {
                return;
            }

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

        void ApplyRoutingMode(InputRoutingMode mode)
        {
            hook.SuppressKeyboardInput = mode == InputRoutingMode.ArduinoOnly;
            form.SetRoutingMode(mode);

            var releaseReport = builder.ReleaseAll();
            lastSentReport = releaseReport.ToArray();
            lock (reportLock)
            {
                latestHeldReport = null;
            }

            if (mode == InputRoutingMode.HostOnly)
            {
                _ = bleServer.PublishReportAsync(releaseReport);
            }

            Console.WriteLine(mode == InputRoutingMode.ArduinoOnly
                ? "[mode] host blocked / Arduino allowed"
                : "[mode] host allowed / Arduino blocked");
        }

        hook.Start();
        Console.WriteLine("Keyboard hook installed. Waiting for key events...");

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }

        await bleServer.PublishReportAsync(builder.ReleaseAll());
        await heartbeatTask;
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
