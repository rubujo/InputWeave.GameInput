using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputReadingSnapshotExtensionsTests
{
    [TestMethod]
    public void GamepadIsButtonDownChecksAllSpecifiedFlags()
    {
        GamepadReadingSnapshot snapshot = CreateGamepadSnapshot(GameInputGamepadButtons.GameInputGamepadA | GameInputGamepadButtons.GameInputGamepadB);

        Assert.IsTrue(snapshot.IsButtonDown(GameInputGamepadButtons.GameInputGamepadA));
        Assert.IsTrue(snapshot.IsButtonDown(GameInputGamepadButtons.GameInputGamepadA | GameInputGamepadButtons.GameInputGamepadB));
        Assert.IsFalse(snapshot.IsButtonDown(GameInputGamepadButtons.GameInputGamepadX));
        Assert.IsFalse(snapshot.IsButtonDown(GameInputGamepadButtons.GameInputGamepadA | GameInputGamepadButtons.GameInputGamepadX));
    }

    [TestMethod]
    public void GamepadIsButtonDownReturnsFalseForNoneFlag()
    {
        GamepadReadingSnapshot snapshot = CreateGamepadSnapshot(GameInputGamepadButtons.GameInputGamepadA);

        Assert.IsFalse(snapshot.IsButtonDown(GameInputGamepadButtons.GameInputGamepadNone));
    }

    [TestMethod]
    public void GamepadWasButtonPressedDetectsRisingEdgeOnly()
    {
        GamepadReadingSnapshot previous = CreateGamepadSnapshot(GameInputGamepadButtons.GameInputGamepadNone);
        GamepadReadingSnapshot current = CreateGamepadSnapshot(GameInputGamepadButtons.GameInputGamepadA);

        Assert.IsTrue(current.WasButtonPressed(previous, GameInputGamepadButtons.GameInputGamepadA));
        Assert.IsFalse(current.WasButtonPressed(current, GameInputGamepadButtons.GameInputGamepadA));
        Assert.IsFalse(previous.WasButtonPressed(current, GameInputGamepadButtons.GameInputGamepadA));
    }

    [TestMethod]
    public void GamepadWasButtonReleasedDetectsFallingEdgeOnly()
    {
        GamepadReadingSnapshot previous = CreateGamepadSnapshot(GameInputGamepadButtons.GameInputGamepadA);
        GamepadReadingSnapshot current = CreateGamepadSnapshot(GameInputGamepadButtons.GameInputGamepadNone);

        Assert.IsTrue(current.WasButtonReleased(previous, GameInputGamepadButtons.GameInputGamepadA));
        Assert.IsFalse(previous.WasButtonReleased(previous, GameInputGamepadButtons.GameInputGamepadA));
        Assert.IsFalse(previous.WasButtonReleased(current, GameInputGamepadButtons.GameInputGamepadA));
    }

    [TestMethod]
    public void KeyboardKeyEdgeDetectionMatchesVirtualKey()
    {
        KeyboardReadingSnapshot previous = new(1, []);
        KeyboardReadingSnapshot current = new(2, [new GameInputKeyState { ScanCode = 30, VirtualKey = 0x41 }]);

        Assert.IsTrue(current.IsKeyDown(0x41));
        Assert.IsFalse(current.IsKeyDown(0x42));
        Assert.IsTrue(current.WasKeyPressed(previous, 0x41));
        Assert.IsFalse(current.WasKeyPressed(current, 0x41));
        Assert.IsTrue(previous.WasKeyReleased(current, 0x41));
        Assert.IsFalse(current.WasKeyReleased(previous, 0x41));
    }

    [TestMethod]
    public void ControllerButtonEdgeDetectionUsesIndexAndToleratesOutOfRange()
    {
        ControllerReadingSnapshot previous = new(1, [], [false, false], []);
        ControllerReadingSnapshot current = new(2, [], [false, true], []);

        Assert.IsTrue(current.IsButtonDown(1));
        Assert.IsFalse(current.IsButtonDown(0));
        Assert.IsFalse(current.IsButtonDown(5), "索引超出裝置按鈕數量時應視為未按下。");
        Assert.IsFalse(current.IsButtonDown(-1), "負索引應視為未按下。");
        Assert.IsTrue(current.WasButtonPressed(previous, 1));
        Assert.IsFalse(current.WasButtonPressed(current, 1));
        Assert.IsTrue(previous.WasButtonReleased(current, 1));
    }

    [TestMethod]
    public void MouseButtonEdgeDetectionDetectsRisingAndFallingEdges()
    {
        MouseReadingSnapshot previous = new(1, new GameInputMouseState { Buttons = GameInputMouseButtons.GameInputMouseNone });
        MouseReadingSnapshot current = new(2, new GameInputMouseState { Buttons = GameInputMouseButtons.GameInputMouseLeftButton });

        Assert.IsTrue(current.IsButtonDown(GameInputMouseButtons.GameInputMouseLeftButton));
        Assert.IsFalse(current.IsButtonDown(GameInputMouseButtons.GameInputMouseNone));
        Assert.IsTrue(current.WasButtonPressed(previous, GameInputMouseButtons.GameInputMouseLeftButton));
        Assert.IsTrue(previous.WasButtonReleased(current, GameInputMouseButtons.GameInputMouseLeftButton));
    }

    private static GamepadReadingSnapshot CreateGamepadSnapshot(GameInputGamepadButtons buttons)
    {
        return new GamepadReadingSnapshot(1, new GameInputGamepadState { Buttons = buttons });
    }
}
