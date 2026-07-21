using System.Reflection;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputCoverageTests
{
    [TestMethod]
    public void ProjectVersionUsesNuGetZeroZeroOne()
    {
        string project = File.ReadAllText(FindRepoFile("src/InputWeave.GameInput/InputWeave.GameInput.csproj"));

        Assert.Contains("<Version>0.0.1</Version>", project);
        Assert.IsFalse(project.Contains("v.0.0.1", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CoverageReportStatesNoGapsForVersionZeroZeroOne()
    {
        string report = File.ReadAllText(FindRepoFile("docs/gameinput-api-coverage.md"));

        Assert.Contains("InputWeave.GameInput v0.0.1", report);
        Assert.Contains("Microsoft.GameInput 3.4.259", report);
        Assert.Contains("缺口：0", report);
        Assert.IsFalse(report.Contains("v.0.0.1", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GeneratedInteropSurfaceHasWrapperCoverageReport()
    {
        string manifest = File.ReadAllText(FindRepoFile("src/InputWeave.GameInput/Interop/Generated/gameinput-abi-manifest.json"));
        string report = File.ReadAllText(FindRepoFile("docs/gameinput-api-coverage.md"));

        Assert.Contains("\"apiVersion\": 3", manifest);
        Assert.Contains("列舉：27 / 27", report);
        Assert.Contains("結構：32 / 32", report);
        Assert.Contains("回呼委派：4 / 4", report);
        Assert.Contains("COM Interface：7 / 7", report);
        Assert.Contains("HRESULT：10 / 10", report);
    }

    [TestMethod]
    public void ForceFeedbackBuilderProducesExpectedKinds()
    {
        GameInputForceFeedbackEnvelope envelope = GameInputForceFeedback.Envelope();
        GameInputForceFeedbackMagnitude magnitude = GameInputForceFeedback.Magnitude(normal: 0.5f);
        GameInputForceFeedbackParams constant = GameInputForceFeedback.Constant(magnitude, envelope);
        GameInputForceFeedbackParams sine = GameInputForceFeedback.SineWave(magnitude, envelope, frequency: 10);
        GameInputForceFeedbackParams square = GameInputForceFeedback.SquareWave(magnitude, envelope, frequency: 20);
        GameInputForceFeedbackParams triangle = GameInputForceFeedback.TriangleWave(magnitude, envelope, frequency: 30);
        GameInputForceFeedbackParams sawtoothUp = GameInputForceFeedback.SawtoothUpWave(magnitude, envelope, frequency: 40);
        GameInputForceFeedbackParams sawtoothDown = GameInputForceFeedback.SawtoothDownWave(magnitude, envelope, frequency: 50);
        GameInputForceFeedbackConditionParams condition = new()
        {
            Magnitude = magnitude
        };

        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackConstant, constant.Kind);
        Assert.AreEqual(0.5f, constant.Constant.Magnitude.Normal);
        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSineWave, sine.Kind);
        Assert.AreEqual(10, sine.SineWave.Frequency);
        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSquareWave, square.Kind);
        Assert.AreEqual(20, square.SquareWave.Frequency);
        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackTriangleWave, triangle.Kind);
        Assert.AreEqual(30, triangle.TriangleWave.Frequency);
        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSawtoothUpWave, sawtoothUp.Kind);
        Assert.AreEqual(40, sawtoothUp.SawtoothUpWave.Frequency);
        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSawtoothDownWave, sawtoothDown.Kind);
        Assert.AreEqual(50, sawtoothDown.SawtoothDownWave.Frequency);
        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSpring, GameInputForceFeedback.Spring(condition).Kind);
        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackFriction, GameInputForceFeedback.Friction(condition).Kind);
        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackDamper, GameInputForceFeedback.Damper(condition).Kind);
        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackInertia, GameInputForceFeedback.Inertia(condition).Kind);
    }

    [TestMethod]
    public void ForceFeedbackBuilderRejectsInvalidUnionKinds()
    {
        GameInputForceFeedbackEnvelope envelope = GameInputForceFeedback.Envelope();
        GameInputForceFeedbackMagnitude magnitude = GameInputForceFeedback.Magnitude(normal: 0.5f);
        GameInputForceFeedbackConditionParams condition = new()
        {
            Magnitude = magnitude
        };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => GameInputForceFeedback.Periodic(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSpring, magnitude, envelope, frequency: 10));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => GameInputForceFeedback.Condition(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSineWave, condition));
    }

    [TestMethod]
    public void SnapshotTypesCopyManagedData()
    {
        GameInputKeyState[] keys = [new GameInputKeyState { ScanCode = 1 }];
        KeyboardReadingSnapshot keyboard = new(100, keys);
        keys[0] = new GameInputKeyState { ScanCode = 2 };

        float[] axes = [0.25f];
        bool[] buttons = [true];
        GameInputSwitchPosition[] switches = [GameInputSwitchPosition.GameInputSwitchUp];
        ControllerReadingSnapshot controller = new(101, axes, buttons, switches);
        axes[0] = 0.75f;
        buttons[0] = false;
        switches[0] = GameInputSwitchPosition.GameInputSwitchDown;

        byte[] rawData = [1, 2, 3];
        RawDeviceReportSnapshot raw = new(102, new GameInputRawDeviceReportInfo { Id = 7 }, rawData);
        rawData[0] = 9;
        byte[] rawCopy = raw.GetData();
        rawCopy[1] = 8;

        Assert.AreEqual(100UL, keyboard.Timestamp);
        Assert.AreEqual(1u, keyboard.Keys[0].ScanCode);
        Assert.AreEqual(101UL, controller.Timestamp);
        Assert.AreEqual(0.25f, controller.Axes[0]);
        Assert.IsTrue(controller.Buttons[0]);
        Assert.AreEqual(GameInputSwitchPosition.GameInputSwitchUp, controller.Switches[0]);
        Assert.AreEqual(102UL, raw.Timestamp);
        Assert.AreEqual(7u, raw.Info.Id);
        Assert.AreEqual(1, raw.Data[0]);
        Assert.AreEqual(2, raw.Data[1]);
    }

    [TestMethod]
    public void ManagerDeviceSelectionUsesSnapshotCapabilities()
    {
        GameInputDeviceInfoSnapshot keyboard = CreateDeviceSnapshot(GameInputKind.GameInputKindKeyboard, GameInputRumbleMotors.GameInputRumbleNone);
        GameInputDeviceInfoSnapshot gamepad = CreateDeviceSnapshot(GameInputKind.GameInputKindGamepad, GameInputRumbleMotors.GameInputRumbleLowFrequency);
        GameInputDeviceInfoSnapshot mouse = CreateDeviceSnapshot(GameInputKind.GameInputKindMouse, GameInputRumbleMotors.GameInputRumbleNone);
        GameInputDeviceInfoSnapshot[] snapshots = [keyboard, gamepad, mouse];

        Assert.AreEqual(1, GameInputDeviceManager.FindFirstDeviceIndex(snapshots, GameInputKind.GameInputKindGamepad));
        Assert.AreEqual(0, GameInputDeviceManager.FindFirstDeviceIndex(snapshots, GameInputKind.GameInputKindKeyboard));
        Assert.AreEqual(2, GameInputDeviceManager.FindFirstDeviceIndex(snapshots, GameInputKind.GameInputKindMouse));
        Assert.AreEqual(-1, GameInputDeviceManager.FindFirstDeviceIndex(snapshots, GameInputKind.GameInputKindSensors));
        Assert.AreEqual(1, GameInputDeviceManager.FindFirstRumbleDeviceIndex(snapshots));
    }

    [TestMethod]
    public void RumbleHelpersMaskUnsupportedMotorsAndScopeClears()
    {
        GameInputRumbleParams requested = GameInputForceFeedback.Rumble(0.1f, 0.2f, 0.3f, 0.4f);
        GameInputRumbleParams masked = GameInputDevice.MaskRumble(
            requested,
            GameInputRumbleMotors.GameInputRumbleLowFrequency | GameInputRumbleMotors.GameInputRumbleRightTrigger);
        int clearCount = 0;

        using (new GameInputRumbleScope(() => clearCount++))
        {
        }

        Assert.AreEqual(0.1f, masked.LowFrequency);
        Assert.AreEqual(0, masked.HighFrequency);
        Assert.AreEqual(0, masked.LeftTrigger);
        Assert.AreEqual(0.4f, masked.RightTrigger);
        Assert.IsTrue(GameInputDevice.HasActiveRumble(masked));
        Assert.IsFalse(GameInputDevice.HasActiveRumble(default));
        Assert.AreEqual(1, clearCount);
    }

    [TestMethod]
    public void ForceFeedbackCapabilityCheckUsesMotorFlags()
    {
        GameInputForceFeedbackMotorInfo motor = new()
        {
            IsSineWaveEffectSupported = true,
            IsSpringEffectSupported = true
        };

        Assert.IsTrue(GameInputDevice.IsForceFeedbackEffectSupported(motor, GameInputForceFeedbackEffectKind.GameInputForceFeedbackSineWave));
        Assert.IsTrue(GameInputDevice.IsForceFeedbackEffectSupported(motor, GameInputForceFeedbackEffectKind.GameInputForceFeedbackSpring));
        Assert.IsFalse(GameInputDevice.IsForceFeedbackEffectSupported(motor, GameInputForceFeedbackEffectKind.GameInputForceFeedbackConstant));
    }

    [TestMethod]
    public void PublicDisposableTypesManageNativeLifetime()
    {
        Type[] disposableTypes =
        [
                typeof(GameInputClient),
                typeof(GameInputDevice),
                typeof(GameInputDispatcher),
                typeof(GameInputMapper),
                typeof(GameInputRawDeviceReport),
                typeof(GameInputForceFeedbackEffect),
                typeof(GameInputCallbackRegistration),
                typeof(GameInputReading),
                typeof(GameInputDeviceManager),
                typeof(GameInputDispatcherWaitHandle)
            ];

        foreach (Type type in disposableTypes)
        {
            Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(type), $"{type.Name} 應明確管理 native lifetime。");
            Assert.IsNotNull(type.GetMethod(nameof(IDisposable.Dispose), BindingFlags.Instance | BindingFlags.Public), $"{type.Name} 應有公開 Dispose。");
        }
    }

    private static string FindRepoFile(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("找不到 repo 檔案。", relativePath);
    }

    private static GameInputDeviceInfoSnapshot CreateDeviceSnapshot(GameInputKind supportedInput, GameInputRumbleMotors supportedRumbleMotors)
    {
        return new GameInputDeviceInfoSnapshot(
            new GameInputDeviceInfo
            {
                SupportedInput = supportedInput,
                SupportedRumbleMotors = supportedRumbleMotors
            },
            displayName: null,
            pnpPath: null,
            keyboard: null,
            mouse: null,
            sensors: null,
            controller: null,
            arcadeStick: null,
            flightStick: null,
            gamepad: null,
            racingWheel: null,
            forceFeedbackMotors: [],
            inputReports: [],
            outputReports: []);
    }
}
