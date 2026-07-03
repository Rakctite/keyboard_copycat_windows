namespace KeyboardCopycat.Windows.Input;

public readonly record struct HidReport(byte[] Bytes)
{
    public const int Size = 8;

    public HidReport(byte modifier, IReadOnlyList<byte> keys)
        : this(BuildBytes(modifier, keys))
    {
    }

    public byte[] ToArray()
    {
        var copy = new byte[Size];
        Array.Copy(Bytes, copy, Size);
        return copy;
    }

    private static byte[] BuildBytes(byte modifier, IReadOnlyList<byte> keys)
    {
        var bytes = new byte[Size];
        bytes[0] = modifier;

        var count = Math.Min(6, keys.Count);
        for (var i = 0; i < count; i++)
        {
            bytes[i + 2] = keys[i];
        }

        return bytes;
    }
}
