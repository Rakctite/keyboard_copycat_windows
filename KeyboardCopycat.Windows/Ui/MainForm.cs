namespace KeyboardCopycat.Windows.Ui;

public sealed class MainForm : Form
{
    private const int MaxLogLines = 5;

    private readonly Label statusLabel;
    private readonly Label inputModeLabel;
    private readonly TextBox logTextBox;
    private readonly Queue<string> logLines = new();

    public MainForm()
    {
        Text = "Keyboard Copycat";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(520, 260);
        Size = new Size(640, 300);

        statusLabel = new Label
        {
            AutoSize = true,
            Text = "Arduino: 대기중",
        };

        inputModeLabel = new Label
        {
            AutoSize = true,
            Text = "Windows 입력: 허용",
        };

        var lockButton = new Button
        {
            AutoSize = true,
            Text = "키보드 입력 잠금",
        };
        lockButton.Click += (_, _) => LockInputRequested?.Invoke(this, EventArgs.Empty);

        var unlockButton = new Button
        {
            AutoSize = true,
            Text = "키보드 입력 허용",
        };
        unlockButton.Click += (_, _) => UnlockInputRequested?.Invoke(this, EventArgs.Empty);

        logTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font(FontFamily.GenericMonospace, 9F),
        };

        var statusPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(12, 12, 12, 4),
            WrapContents = false,
        };
        statusPanel.Controls.Add(statusLabel);
        statusPanel.Controls.Add(new Label { AutoSize = true, Text = "   " });
        statusPanel.Controls.Add(inputModeLabel);

        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(12, 4, 12, 8),
            WrapContents = false,
        };
        buttonPanel.Controls.Add(lockButton);
        buttonPanel.Controls.Add(unlockButton);

        var logPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 0, 12, 12),
        };
        logPanel.Controls.Add(logTextBox);

        Controls.Add(logPanel);
        Controls.Add(buttonPanel);
        Controls.Add(statusPanel);
    }

    public event EventHandler? LockInputRequested;

    public event EventHandler? UnlockInputRequested;

    public void AppendLog(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(line));
            return;
        }

        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        logLines.Enqueue(line);
        while (logLines.Count > MaxLogLines)
        {
            logLines.Dequeue();
        }

        logTextBox.Lines = logLines.ToArray();
        logTextBox.SelectionStart = logTextBox.TextLength;
        logTextBox.ScrollToCaret();
    }

    public void SetArduinoConnected(bool connected)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetArduinoConnected(connected));
            return;
        }

        statusLabel.Text = connected ? "Arduino: 연결됨" : "Arduino: 대기중";
    }

    public void SetInputLocked(bool locked)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetInputLocked(locked));
            return;
        }

        inputModeLabel.Text = locked ? "Windows 입력: 잠금" : "Windows 입력: 허용";
    }
}
