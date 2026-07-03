namespace KeyboardCopycat.Windows.Input;

public static class VirtualKeyMapper
{
    private static readonly IReadOnlyDictionary<int, byte> NonModifierKeys = new Dictionary<int, byte>
    {
        [0x41] = 0x04,
        [0x42] = 0x05,
        [0x43] = 0x06,
        [0x44] = 0x07,
        [0x45] = 0x08,
        [0x46] = 0x09,
        [0x47] = 0x0A,
        [0x48] = 0x0B,
        [0x49] = 0x0C,
        [0x4A] = 0x0D,
        [0x4B] = 0x0E,
        [0x4C] = 0x0F,
        [0x4D] = 0x10,
        [0x4E] = 0x11,
        [0x4F] = 0x12,
        [0x50] = 0x13,
        [0x51] = 0x14,
        [0x52] = 0x15,
        [0x53] = 0x16,
        [0x54] = 0x17,
        [0x55] = 0x18,
        [0x56] = 0x19,
        [0x57] = 0x1A,
        [0x58] = 0x1B,
        [0x59] = 0x1C,
        [0x5A] = 0x1D,
        [VirtualKeyCodes.One] = 0x1E,
        [VirtualKeyCodes.Two] = 0x1F,
        [VirtualKeyCodes.Three] = 0x20,
        [VirtualKeyCodes.Four] = 0x21,
        [VirtualKeyCodes.Five] = 0x22,
        [VirtualKeyCodes.Six] = 0x23,
        [VirtualKeyCodes.Seven] = 0x24,
        [VirtualKeyCodes.Eight] = 0x25,
        [VirtualKeyCodes.Nine] = 0x26,
        [VirtualKeyCodes.Zero] = 0x27,
        [VirtualKeyCodes.Enter] = 0x28,
        [VirtualKeyCodes.Escape] = 0x29,
        [VirtualKeyCodes.Backspace] = 0x2A,
        [VirtualKeyCodes.Tab] = 0x2B,
        [VirtualKeyCodes.Space] = 0x2C,
        [VirtualKeyCodes.Minus] = 0x2D,
        [VirtualKeyCodes.Equal] = 0x2E,
        [VirtualKeyCodes.LeftBracket] = 0x2F,
        [VirtualKeyCodes.RightBracket] = 0x30,
        [VirtualKeyCodes.Backslash] = 0x31,
        [VirtualKeyCodes.Semicolon] = 0x33,
        [VirtualKeyCodes.Quote] = 0x34,
        [VirtualKeyCodes.Backquote] = 0x35,
        [VirtualKeyCodes.Comma] = 0x36,
        [VirtualKeyCodes.Period] = 0x37,
        [VirtualKeyCodes.Slash] = 0x38,
        [VirtualKeyCodes.F1] = 0x3A,
        [VirtualKeyCodes.F2] = 0x3B,
        [VirtualKeyCodes.F3] = 0x3C,
        [VirtualKeyCodes.F4] = 0x3D,
        [VirtualKeyCodes.F5] = 0x3E,
        [VirtualKeyCodes.F6] = 0x3F,
        [VirtualKeyCodes.F7] = 0x40,
        [VirtualKeyCodes.F8] = 0x41,
        [VirtualKeyCodes.F9] = 0x42,
        [VirtualKeyCodes.F10] = 0x43,
        [VirtualKeyCodes.F11] = 0x44,
        [VirtualKeyCodes.F12] = 0x45,
        [VirtualKeyCodes.RightArrow] = 0x4F,
        [VirtualKeyCodes.LeftArrow] = 0x50,
        [VirtualKeyCodes.DownArrow] = 0x51,
        [VirtualKeyCodes.UpArrow] = 0x52,
    };

    private static readonly IReadOnlyDictionary<int, byte> Modifiers = new Dictionary<int, byte>
    {
        [VirtualKeyCodes.LeftControl] = 0x01,
        [VirtualKeyCodes.LeftShift] = 0x02,
        [VirtualKeyCodes.LeftAlt] = 0x04,
        [VirtualKeyCodes.LeftWindows] = 0x08,
        [VirtualKeyCodes.RightControl] = 0x10,
        [VirtualKeyCodes.RightShift] = 0x20,
        [VirtualKeyCodes.RightAlt] = 0x40,
        [VirtualKeyCodes.RightWindows] = 0x80,
    };

    public static bool TryMapKey(int virtualKey, out byte usageId)
    {
        return NonModifierKeys.TryGetValue(virtualKey, out usageId);
    }

    public static bool TryMapModifier(int virtualKey, out byte modifierMask)
    {
        return Modifiers.TryGetValue(virtualKey, out modifierMask);
    }
}
