namespace KeyboardCopycat.Windows.Input;

public sealed class HidReportBuilder
{
    private readonly List<byte> pressedKeys = [];
    private byte modifiers;

    public HidReport KeyDown(int virtualKey)
    {
        if (VirtualKeyMapper.TryMapModifier(virtualKey, out var modifierMask))
        {
            modifiers |= modifierMask;
            return CurrentReport();
        }

        if (VirtualKeyMapper.TryMapKey(virtualKey, out var usageId) &&
            !pressedKeys.Contains(usageId) &&
            pressedKeys.Count < 6)
        {
            pressedKeys.Add(usageId);
        }

        return CurrentReport();
    }

    public HidReport KeyUp(int virtualKey)
    {
        if (VirtualKeyMapper.TryMapModifier(virtualKey, out var modifierMask))
        {
            modifiers = (byte)(modifiers & ~modifierMask);
            return CurrentReport();
        }

        if (VirtualKeyMapper.TryMapKey(virtualKey, out var usageId))
        {
            pressedKeys.Remove(usageId);
        }

        return CurrentReport();
    }

    public HidReport ReleaseAll()
    {
        pressedKeys.Clear();
        modifiers = 0;
        return CurrentReport();
    }

    private HidReport CurrentReport()
    {
        return new HidReport(modifiers, pressedKeys);
    }
}
