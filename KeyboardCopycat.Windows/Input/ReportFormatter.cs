namespace KeyboardCopycat.Windows.Input;

public static class ReportFormatter
{
    public static string FormatReport(HidReport report)
    {
        return string.Join(" ", report.ToArray().Select(b => b.ToString("X2")));
    }
}
