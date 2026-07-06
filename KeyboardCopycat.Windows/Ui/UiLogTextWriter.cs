using System.Text;

namespace KeyboardCopycat.Windows.Ui;

public sealed class UiLogTextWriter : TextWriter
{
    private readonly Action<string> appendLine;
    private readonly StringBuilder lineBuffer = new();

    public UiLogTextWriter(Action<string> appendLine)
    {
        this.appendLine = appendLine;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        if (value == '\r')
        {
            return;
        }

        if (value == '\n')
        {
            FlushLine();
            return;
        }

        lineBuffer.Append(value);
    }

    public override void Write(string? value)
    {
        if (value is null)
        {
            return;
        }

        foreach (var character in value)
        {
            Write(character);
        }
    }

    public override void WriteLine(string? value)
    {
        Write(value);
        FlushLine();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FlushLine();
        }

        base.Dispose(disposing);
    }

    private void FlushLine()
    {
        if (lineBuffer.Length == 0)
        {
            return;
        }

        appendLine(lineBuffer.ToString());
        lineBuffer.Clear();
    }
}
