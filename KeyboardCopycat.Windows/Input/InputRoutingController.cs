namespace KeyboardCopycat.Windows.Input;

public enum InputRoutingMode
{
    HostOnly,
    ArduinoOnly,
}

public sealed class InputRoutingController
{
    private readonly HashSet<int> pressedKeys = [];
    private bool chordLatched;

    public InputRoutingMode Mode { get; private set; } = InputRoutingMode.HostOnly;

    public InputRoutingMode? HandleKey(int virtualKey, bool isDown)
    {
        if (isDown)
        {
            pressedKeys.Add(virtualKey);
        }
        else
        {
            pressedKeys.Remove(virtualKey);
        }

        var chordPressed = IsMinusPressed() && IsAsteriskPressed();
        if (!chordPressed)
        {
            chordLatched = false;
            return null;
        }

        if (chordLatched)
        {
            return null;
        }

        chordLatched = true;
        Mode = Mode == InputRoutingMode.HostOnly
            ? InputRoutingMode.ArduinoOnly
            : InputRoutingMode.HostOnly;
        return Mode;
    }

    public bool SetMode(InputRoutingMode mode)
    {
        if (Mode == mode)
        {
            return false;
        }

        Mode = mode;
        return true;
    }

    private bool IsMinusPressed()
    {
        return pressedKeys.Contains(VirtualKeyCodes.Minus) ||
            pressedKeys.Contains(VirtualKeyCodes.Subtract);
    }

    private bool IsAsteriskPressed()
    {
        return pressedKeys.Contains(VirtualKeyCodes.Multiply) ||
            (pressedKeys.Contains(VirtualKeyCodes.Eight) &&
                (pressedKeys.Contains(VirtualKeyCodes.LeftShift) ||
                    pressedKeys.Contains(VirtualKeyCodes.RightShift)));
    }
}
