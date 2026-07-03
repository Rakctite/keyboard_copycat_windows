namespace KeyboardCopycat.Windows.Ble;

public sealed record BleBridgeOptions(
    string DeviceName,
    Guid ServiceUuid,
    Guid ReportCharacteristicUuid)
{
    public static BleBridgeOptions Defaults { get; } = new(
        "KeyboardBridge",
        Guid.Parse("7f2b4c00-7b64-4c0d-9b7a-1e0f3c9a0001"),
        Guid.Parse("7f2b4c01-7b64-4c0d-9b7a-1e0f3c9a0001"));
}
