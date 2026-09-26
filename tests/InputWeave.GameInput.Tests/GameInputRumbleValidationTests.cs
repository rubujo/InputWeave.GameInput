namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputRumbleValidationTests
{
    [TestMethod]
    [DataRow(float.NaN)]
    [DataRow(float.PositiveInfinity)]
    [DataRow(float.NegativeInfinity)]
    [DataRow(-0.01f)]
    [DataRow(1.01f)]
    public void RumbleStrengthOutsideUnitRangeIsRejected(float value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => GameInputDevice.ValidateRumbleStrength(GameInputForceFeedback.Rumble(value, 0)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => GameInputDevice.ValidateRumbleStrength(GameInputForceFeedback.Rumble(0, 0, rightTrigger: value)));
    }

    [TestMethod]
    [DataRow(0f)]
    [DataRow(0.5f)]
    [DataRow(1f)]
    public void RumbleStrengthInsideUnitRangeIsAccepted(float value)
    {
        GameInputDevice.ValidateRumbleStrength(GameInputForceFeedback.Rumble(value, value, value, value));
    }
}
