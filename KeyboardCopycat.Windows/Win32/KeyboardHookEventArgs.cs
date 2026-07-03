namespace KeyboardCopycat.Windows.Win32;

public sealed class KeyboardHookEventArgs : EventArgs
{
    public KeyboardHookEventArgs(int virtualKeyCode, bool isDown)
    {
        VirtualKeyCode = virtualKeyCode;
        IsDown = isDown;
    }

    public int VirtualKeyCode { get; }
    public bool IsDown { get; }
}
