using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputHardwareSmokeTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("Hardware")]
    public void Api4UsbXboxControllerExposesGettersAndReading()
    {
        if (Environment.GetEnvironmentVariable("INPUTWEAVE_GAMEINPUT_XBOX_USB_TESTS") != "1")
        {
            Assert.Inconclusive("Xbox USB 測試需設定 INPUTWEAVE_GAMEINPUT_XBOX_USB_TESTS=1，並接上 USB Xbox One 控制器。");
        }
        using GameInputClient client = GameInputClient.Create();
        client.EnableBackgroundInput(true);
        IReadOnlyList<GameInputDevice> devices = client.EnumerateDevices(GameInputKind.GameInputKindGamepad);
        try
        {
            GameInputDevice? controller = devices.FirstOrDefault(device => device.GetVendorId() == 0x045E);
            Assert.IsNotNull(controller, "必須實際探索到 Microsoft Xbox 控制器。");
            Assert.IsTrue(controller.IsConnected());
            Assert.AreEqual((ushort)0x02EA, controller.GetProductId(), "此驗收使用已連線的 USB Xbox One 控制器。");
            GameInputDeviceInfoSnapshot snapshot = controller.GetDeviceInfoSnapshot();
            Assert.AreEqual(controller.GetVendorId(), snapshot.VendorId);
            Assert.AreEqual(controller.GetProductId(), snapshot.ProductId);
            Assert.IsNotNull(snapshot.Gamepad);
            Assert.IsFalse(string.IsNullOrEmpty(snapshot.DisplayName));
            Assert.IsFalse(string.IsNullOrEmpty(snapshot.PnpPath));
            _ = controller.GetControllerAxisLabels();
            _ = controller.GetControllerButtonLabels();
            _ = controller.GetControllerSwitchInfo();
            _ = controller.GetForceFeedbackMotorInfo();
            _ = controller.GetInputReportInfo();
            _ = controller.GetOutputReportInfo();
            _ = controller.GetFeatureReportInfo();
            Assert.IsTrue(controller.TryGetXInputDeviceIndex(out uint xinput));
            using GameInputReading? reading = client.GetCurrentReading(GameInputKind.GameInputKindGamepad, controller);
            Assert.IsNotNull(reading);
            Assert.IsTrue(reading.TryGetGamepadState(out GameInputGamepadState state));
            _ = reading.TryGetSystemButtonState(out _);
            TestContext.WriteLine($"API {GameInputConstants.ApiVersion}；{snapshot.DisplayName}；VID/PID={snapshot.VendorId:X4}:{snapshot.ProductId:X4}；XInput={xinput}；Buttons={state.Buttons}；Runtime={GameInputRuntime.GetInfo().FileVersion}");
        }
        finally
        {
            foreach (GameInputDevice device in devices)
            {
                device.Dispose();
            }
        }
    }

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
