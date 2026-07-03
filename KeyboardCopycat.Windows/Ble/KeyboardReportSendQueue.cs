using System.Threading.Channels;
using KeyboardCopycat.Windows.Input;

namespace KeyboardCopycat.Windows.Ble;

public sealed class KeyboardReportSendQueue : IDisposable
{
    private readonly Channel<HidReport> channel = Channel.CreateUnbounded<HidReport>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    private readonly CancellationTokenSource stop = new();
    private readonly Task worker;

    public KeyboardReportSendQueue(BleKeyboardBridgeClient client, CancellationToken appCancellationToken)
    {
        worker = Task.Run(async () =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                stop.Token,
                appCancellationToken);

            await foreach (var report in channel.Reader.ReadAllAsync(linked.Token))
            {
                await client.SendReportAsync(report, linked.Token);
                Console.WriteLine($"[ble] wrote report={ReportFormatter.FormatReport(report)}");
            }
        }, CancellationToken.None);
    }

    public void Enqueue(HidReport report)
    {
        channel.Writer.TryWrite(report);
    }

    public async Task FlushAsync()
    {
        await Task.Delay(50);
    }

    public void Dispose()
    {
        channel.Writer.TryComplete();
        stop.Cancel();
        try
        {
            worker.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Shutdown should not hide the original console exit path.
        }
        stop.Dispose();
    }
}
