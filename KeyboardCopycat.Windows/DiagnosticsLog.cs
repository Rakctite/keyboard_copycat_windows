namespace KeyboardCopycat.Windows;

internal static class DiagnosticsLog
{
    private static readonly object Sync = new();

    public static string FilePath { get; } = Path.Combine(
        AppContext.BaseDirectory,
        "copycat-diagnostics.log");

    public static void Start()
    {
        lock (Sync)
        {
            File.WriteAllText(
                FilePath,
                $"[{DateTimeOffset.Now:O}] Keyboard Copycat diagnostics started{Environment.NewLine}");
        }
    }

    public static void Write(string message)
    {
        var line = $"[{DateTimeOffset.Now:O}] {message}";
        lock (Sync)
        {
            File.AppendAllText(FilePath, line + Environment.NewLine);
        }

        Console.WriteLine(line);
    }
}
