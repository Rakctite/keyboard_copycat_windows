using KeyboardCopycat.Windows.Input;

namespace KeyboardCopycat.Windows.Tests;

public sealed class InputRoutingControllerTests
{
    [Fact]
    public void StartsWithHostAllowedAndArduinoBlocked()
    {
        var controller = new InputRoutingController();

        Assert.Equal(InputRoutingMode.HostOnly, controller.Mode);
    }

    [Fact]
    public void NumpadMinusAndMultiplyToggleBetweenExclusiveModes()
    {
        var controller = new InputRoutingController();

        Assert.Null(controller.HandleKey(VirtualKeyCodes.Subtract, true));
        Assert.Equal(
            InputRoutingMode.ArduinoOnly,
            controller.HandleKey(VirtualKeyCodes.Multiply, true));

        controller.HandleKey(VirtualKeyCodes.Multiply, false);
        controller.HandleKey(VirtualKeyCodes.Subtract, false);
        controller.HandleKey(VirtualKeyCodes.Multiply, true);

        Assert.Equal(
            InputRoutingMode.HostOnly,
            controller.HandleKey(VirtualKeyCodes.Subtract, true));
    }

    [Fact]
    public void MainMinusAndShiftEightAlsoToggleMode()
    {
        var controller = new InputRoutingController();

        controller.HandleKey(VirtualKeyCodes.Minus, true);
        controller.HandleKey(VirtualKeyCodes.LeftShift, true);

        Assert.Equal(
            InputRoutingMode.ArduinoOnly,
            controller.HandleKey(VirtualKeyCodes.Eight, true));
    }

    [Fact]
    public void HeldChordOnlyChangesModeOnce()
    {
        var controller = new InputRoutingController();
        controller.HandleKey(VirtualKeyCodes.Subtract, true);
        controller.HandleKey(VirtualKeyCodes.Multiply, true);

        Assert.Null(controller.HandleKey(VirtualKeyCodes.Multiply, true));
        Assert.Equal(InputRoutingMode.ArduinoOnly, controller.Mode);
    }
}
