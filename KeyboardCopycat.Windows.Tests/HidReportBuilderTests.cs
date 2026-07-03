using KeyboardCopycat.Windows.Input;

namespace KeyboardCopycat.Windows.Tests;

public sealed class HidReportBuilderTests
{
    [Fact]
    public void PressingAProducesUsbHidAReport()
    {
        var builder = new HidReportBuilder();

        var report = builder.KeyDown(VirtualKeyCodes.A);

        Assert.Equal(new byte[] { 0, 0, 0x04, 0, 0, 0, 0, 0 }, report.ToArray());
    }

    [Fact]
    public void ReleasingLastKeyProducesAllZeroReport()
    {
        var builder = new HidReportBuilder();

        builder.KeyDown(VirtualKeyCodes.A);
        var report = builder.KeyUp(VirtualKeyCodes.A);

        Assert.Equal(new byte[8], report.ToArray());
    }

    [Fact]
    public void PressingLeftShiftWithAProducesModifierReport()
    {
        var builder = new HidReportBuilder();

        builder.KeyDown(VirtualKeyCodes.LeftShift);
        var report = builder.KeyDown(VirtualKeyCodes.A);

        Assert.Equal(new byte[] { 0x02, 0, 0x04, 0, 0, 0, 0, 0 }, report.ToArray());
    }

    [Fact]
    public void PressingMoreThanSixNonModifierKeysKeepsFirstSix()
    {
        var builder = new HidReportBuilder();

        builder.KeyDown(VirtualKeyCodes.A);
        builder.KeyDown(VirtualKeyCodes.B);
        builder.KeyDown(VirtualKeyCodes.C);
        builder.KeyDown(VirtualKeyCodes.D);
        builder.KeyDown(VirtualKeyCodes.E);
        builder.KeyDown(VirtualKeyCodes.F);
        var report = builder.KeyDown(VirtualKeyCodes.G);

        Assert.Equal(new byte[] { 0, 0, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 }, report.ToArray());
    }

    [Fact]
    public void UnsupportedKeysDoNotChangeCurrentReport()
    {
        var builder = new HidReportBuilder();

        builder.KeyDown(VirtualKeyCodes.A);
        var report = builder.KeyDown(0xE7);

        Assert.Equal(new byte[] { 0, 0, 0x04, 0, 0, 0, 0, 0 }, report.ToArray());
    }

    [Fact]
    public void CommonSymbolAndNavigationKeysMapToUsbHidUsages()
    {
        var builder = new HidReportBuilder();

        builder.KeyDown(VirtualKeyCodes.One);
        builder.KeyDown(VirtualKeyCodes.Minus);
        builder.KeyDown(VirtualKeyCodes.RightArrow);
        var report = builder.KeyDown(VirtualKeyCodes.F1);

        Assert.Equal(new byte[] { 0, 0, 0x1E, 0x2D, 0x4F, 0x3A, 0, 0 }, report.ToArray());
    }
}
