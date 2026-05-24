namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputHardwareSmokeTests
{
    [TestMethod]
    [TestCategory("Hardware")]
    public void HardwareSmokeCoversManagerDispatcherAndReadingPaths()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS"), "1", StringComparison.Ordinal))
        {
            Assert.Inconclusive("硬體 smoke 測試需設定 INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS=1。");
        }

        try
        {
            using GameInputDeviceManager manager = GameInputDeviceManager.Create();
            _ = manager.RefreshDevices();
            _ = manager.GetCurrentGamepad();
            using GameInputClient client = GameInputClient.Create();
            using GameInputDispatcher dispatcher = client.CreateDispatcher();
            _ = dispatcher.Dispatch(0);
        }
        catch (DllNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境未載入 GameInput.dll：{ex.Message}");
        }
        catch (EntryPointNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境的 GameInput.dll 不含必要進入點：{ex.Message}");
        }
    }
}
