using System.Reflection;
using System.Text.Json;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputSurfaceTests
{
    [TestMethod]
    public void PublicWrapperTypesCoverCoreWrapperSurface()
    {
        Type[] requiredTypes =
        [
                typeof(GameInputClient),
                typeof(GameInputDevice),
                typeof(GameInputDispatcher),
                typeof(GameInputMapper),
                typeof(GameInputRawDeviceReport),
                typeof(GameInputForceFeedbackEffect),
                typeof(GameInputCallbackRegistration),
                typeof(GameInputReading),
                typeof(GameInputForceFeedback),
                typeof(GameInputRumbleScope),
                typeof(GameInputDeviceManager),
                typeof(GameInputDeviceInfoSnapshot),
                typeof(GameInputHapticInfoSnapshot),
                typeof(KeyboardReadingSnapshot),
                typeof(MouseReadingSnapshot),
                typeof(SensorsReadingSnapshot),
                typeof(ControllerReadingSnapshot),
                typeof(ArcadeStickReadingSnapshot),
                typeof(FlightStickReadingSnapshot),
                typeof(RacingWheelReadingSnapshot),
                typeof(RawDeviceReportSnapshot),
                typeof(GameInputRuntime),
                typeof(GameInputRuntimeInfo),
                typeof(GameInputRuntimeProbeInfo),
                typeof(GameInputRuntimeCandidateInfo),
                typeof(GameInputRuntimeModuleKind)
            ];

        foreach (Type type in requiredTypes)
        {
            Assert.IsTrue(type.IsPublic, $"{type.Name} 應為公開 wrapper 型別。");
        }
    }

    [TestMethod]
    public void GameInputClientExposesCoreWrapperMethods()
    {
        string[] methodNames =
        [
                nameof(GameInputClient.GetCurrentReading),
                nameof(GameInputClient.GetCurrentGamepad),
                nameof(GameInputClient.GetCurrentKeyboard),
                nameof(GameInputClient.GetCurrentMouse),
                nameof(GameInputClient.GetCurrentSensors),
                nameof(GameInputClient.GetCurrentController),
                nameof(GameInputClient.GetCurrentArcadeStick),
                nameof(GameInputClient.GetCurrentFlightStick),
                nameof(GameInputClient.GetCurrentRacingWheel),
                nameof(GameInputClient.GetCurrentRawReport),
                nameof(GameInputClient.GetNextReading),
                nameof(GameInputClient.GetPreviousReading),
                nameof(GameInputClient.EnumerateDevices),
                nameof(GameInputClient.FindDeviceFromId),
                nameof(GameInputClient.FindDeviceFromPlatformString),
                nameof(GameInputClient.CreateDispatcher),
                nameof(GameInputClient.CreateAggregateDevice),
                nameof(GameInputClient.DisableAggregateDevice),
                nameof(GameInputClient.RegisterReadingCallback),
                nameof(GameInputClient.RegisterDeviceCallback),
                nameof(GameInputClient.RegisterSystemButtonCallback),
                nameof(GameInputClient.RegisterKeyboardLayoutCallback)
            ];

        AssertPublicMethods(typeof(GameInputClient), methodNames);
    }

    [TestMethod]
    public void DeviceAndReadingExposeCoreWrapperMethods()
    {
        AssertPublicMethods(
            typeof(GameInputDevice),
            new[]
            {
                    nameof(GameInputDevice.GetDeviceInfo),
                    nameof(GameInputDevice.GetDeviceInfoSnapshot),
                    nameof(GameInputDevice.GetHapticInfo),
                    nameof(GameInputDevice.GetHapticInfoSnapshot),
                    nameof(GameInputDevice.TryGetHapticInfoSnapshot),
                    nameof(GameInputDevice.CreateForceFeedbackEffect),
                    nameof(GameInputDevice.TryCreateForceFeedbackEffect),
                    nameof(GameInputDevice.IsForceFeedbackEffectSupported),
                    nameof(GameInputDevice.SetRumbleState),
                    nameof(GameInputDevice.TrySetRumble),
                    nameof(GameInputDevice.StartRumbleScope),
                    nameof(GameInputDevice.ClearRumbleState),
                    nameof(GameInputDevice.CreateInputMapper),
                    nameof(GameInputDevice.GetExtraAxisIndexes),
                    nameof(GameInputDevice.GetExtraButtonIndexes),
                    nameof(GameInputDevice.CreateRawDeviceReport),
                    nameof(GameInputDevice.SendRawDeviceOutput)
            });

        AssertPublicMethods(
            typeof(GameInputReading),
            new[]
            {
                    nameof(GameInputReading.GetControllerAxisState),
                    nameof(GameInputReading.GetControllerButtonState),
                    nameof(GameInputReading.GetControllerSwitchState),
                    nameof(GameInputReading.GetKeyState),
                    nameof(GameInputReading.TryGetKeyboardSnapshot),
                    nameof(GameInputReading.TryGetControllerSnapshot),
                    nameof(GameInputReading.TryGetMouseState),
                    nameof(GameInputReading.TryGetMouseSnapshot),
                    nameof(GameInputReading.TryGetSensorsState),
                    nameof(GameInputReading.TryGetSensorsSnapshot),
                    nameof(GameInputReading.TryGetArcadeStickState),
                    nameof(GameInputReading.TryGetArcadeStickSnapshot),
                    nameof(GameInputReading.TryGetFlightStickState),
                    nameof(GameInputReading.TryGetFlightStickSnapshot),
                    nameof(GameInputReading.TryGetGamepadState),
                    nameof(GameInputReading.TryGetGamepadSnapshot),
                    nameof(GameInputReading.TryGetRacingWheelState),
                    nameof(GameInputReading.TryGetRacingWheelSnapshot),
                    nameof(GameInputReading.TryGetRawReport),
                    nameof(GameInputReading.TryGetRawReportSnapshot)
            });
    }

    [TestMethod]
    public void ManagedConvenienceApiCoversVersionZeroZeroOneSurface()
    {
        AssertPublicMethods(
            typeof(GameInputDeviceManager),
            new[]
            {
                    nameof(GameInputDeviceManager.Create),
                    nameof(GameInputDeviceManager.RefreshDevices),
                    nameof(GameInputDeviceManager.StartDeviceEvents),
                    nameof(GameInputDeviceManager.StopDeviceEvents),
                    nameof(GameInputDeviceManager.TryDequeueEvent),
                    nameof(GameInputDeviceManager.GetCurrentReading),
                    nameof(GameInputDeviceManager.GetCurrentGamepad),
                    nameof(GameInputDeviceManager.GetCurrentKeyboard),
                    nameof(GameInputDeviceManager.GetCurrentMouse),
                    nameof(GameInputDeviceManager.GetCurrentSensors),
                    nameof(GameInputDeviceManager.GetCurrentController),
                    nameof(GameInputDeviceManager.GetCurrentArcadeStick),
                    nameof(GameInputDeviceManager.GetCurrentFlightStick),
                    nameof(GameInputDeviceManager.GetCurrentRacingWheel),
                    nameof(GameInputDeviceManager.GetCurrentRawReport),
                    nameof(GameInputDeviceManager.TryGetFirstDevice),
                    nameof(GameInputDeviceManager.TryGetFirstGamepad),
                    nameof(GameInputDeviceManager.TryGetFirstKeyboard),
                    nameof(GameInputDeviceManager.TryGetFirstMouse),
                    nameof(GameInputDeviceManager.TryGetFirstRumbleDevice)
            });

        AssertPublicMethods(
            typeof(GameInputForceFeedback),
            new[]
            {
                    nameof(GameInputForceFeedback.Rumble),
                    nameof(GameInputForceFeedback.Envelope),
                    nameof(GameInputForceFeedback.Magnitude),
                    nameof(GameInputForceFeedback.Constant),
                    nameof(GameInputForceFeedback.Ramp),
                    nameof(GameInputForceFeedback.SineWave),
                    nameof(GameInputForceFeedback.SquareWave),
                    nameof(GameInputForceFeedback.TriangleWave),
                    nameof(GameInputForceFeedback.SawtoothUpWave),
                    nameof(GameInputForceFeedback.SawtoothDownWave),
                    nameof(GameInputForceFeedback.Periodic),
                    nameof(GameInputForceFeedback.Spring),
                    nameof(GameInputForceFeedback.Friction),
                    nameof(GameInputForceFeedback.Damper),
                    nameof(GameInputForceFeedback.Inertia),
                    nameof(GameInputForceFeedback.Condition)
            });

        AssertPublicMethods(
            typeof(GameInputRawDeviceReport),
            new[]
            {
                    nameof(GameInputRawDeviceReport.CopyRawData),
                    nameof(GameInputRawDeviceReport.GetRawData),
                    nameof(GameInputRawDeviceReport.SetRawData)
            });

        AssertPublicMethods(
            typeof(RawDeviceReportSnapshot),
            new[]
            {
                    nameof(RawDeviceReportSnapshot.GetData)
            });

        AssertPublicMethods(
            typeof(GameInputDispatcher),
            new[]
            {
                    nameof(GameInputDispatcher.Dispatch),
                    nameof(GameInputDispatcher.OpenWaitHandle),
                    nameof(GameInputDispatcher.OpenSafeWaitHandle)
            });

        AssertPublicMethods(
            typeof(GameInputRuntime),
            new[]
            {
                    nameof(GameInputRuntime.GetInfo),
                    nameof(GameInputRuntime.TryProbe)
            });

        AssertPublicProperties(
            typeof(GameInputRuntimeInfo),
            new[]
            {
                    nameof(GameInputRuntimeInfo.LoaderPolicy),
                    nameof(GameInputRuntimeInfo.LoadedModuleKind),
                    nameof(GameInputRuntimeInfo.LoadedModulePath),
                    nameof(GameInputRuntimeInfo.FileVersion),
                    nameof(GameInputRuntimeInfo.IsAvailable)
            });

        AssertPublicProperties(
            typeof(GameInputRuntimeProbeInfo),
            new[]
            {
                    nameof(GameInputRuntimeProbeInfo.LoaderPolicy),
                    nameof(GameInputRuntimeProbeInfo.IsAvailable),
                    nameof(GameInputRuntimeProbeInfo.SelectedModuleKind),
                    nameof(GameInputRuntimeProbeInfo.SelectedModulePath),
                    nameof(GameInputRuntimeProbeInfo.SelectedFileVersion),
                    nameof(GameInputRuntimeProbeInfo.HResult),
                    nameof(GameInputRuntimeProbeInfo.Win32Error),
                    nameof(GameInputRuntimeProbeInfo.Candidates)
            });

        AssertPublicProperties(
            typeof(GameInputRuntimeCandidateInfo),
            new[]
            {
                    nameof(GameInputRuntimeCandidateInfo.ModuleKind),
                    nameof(GameInputRuntimeCandidateInfo.ModulePath),
                    nameof(GameInputRuntimeCandidateInfo.Exists),
                    nameof(GameInputRuntimeCandidateInfo.FileVersion),
                    nameof(GameInputRuntimeCandidateInfo.DiscoveryHResult),
                    nameof(GameInputRuntimeCandidateInfo.FileVersionHResult),
                    nameof(GameInputRuntimeCandidateInfo.LoadHResult),
                    nameof(GameInputRuntimeCandidateInfo.GetProcAddressHResult),
                    nameof(GameInputRuntimeCandidateInfo.Win32Error)
            });
    }

    [TestMethod]
    public void AbiManifestContainsCurrentHeaderSurface()
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(FindRepoFile("src/InputWeave.GameInput/Interop/Generated/gameinput-abi-manifest.json")));
        JsonElement root = document.RootElement;

        Assert.AreEqual(3, root.GetProperty("apiVersion").GetInt32());
        Assert.AreEqual(27, root.GetProperty("enums").GetArrayLength());
        Assert.AreEqual(32, root.GetProperty("structs").GetArrayLength());
        Assert.AreEqual(4, root.GetProperty("callbacks").GetArrayLength());
        Assert.AreEqual(7, root.GetProperty("interfaces").GetArrayLength());
        Assert.AreEqual(10, root.GetProperty("hResults").GetArrayLength());

        AssertJsonArrayContains(root.GetProperty("interfaces"), "name", "IGameInput");
        AssertJsonArrayContains(root.GetProperty("interfaces"), "name", "IGameInputReading");
        AssertJsonArrayContains(root.GetProperty("interfaces"), "name", "IGameInputDevice");
        AssertJsonArrayContains(root.GetProperty("callbacks"), "name", "GameInputReadingCallback");
        AssertJsonArrayContains(root.GetProperty("hResults"), "name", "GAMEINPUT_E_INPUT_KIND_NOT_PRESENT");
        AssertJsonArrayContains(root.GetProperty("structs"), "name", "GameInputForceFeedbackParams");

        JsonElement gameInputInterface = FindJsonArrayItem(root.GetProperty("interfaces"), "name", "IGameInput");
        Assert.AreEqual(16, gameInputInterface.GetProperty("methods").GetArrayLength());
    }

    [TestMethod]
    public void RuntimeCreateReportsActionableResult()
    {
        try
        {
            using GameInputClient client = GameInputClient.Create();
            _ = client.GetCurrentTimestamp();
            Assert.IsNotNull(client);
        }
        catch (DllNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境未載入 GameInput.dll：{ex.Message}");
        }
        catch (EntryPointNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境的 GameInput.dll 不含必要進入點：{ex.Message}");
        }
        catch (GameInputException ex)
        {
            Assert.IsLessThan(ex.HResult, 0);
        }
    }

    private static void AssertPublicMethods(Type type, IReadOnlyCollection<string> methodNames)
    {
        HashSet<string> actual = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string methodName in methodNames)
        {
            CollectionAssert.Contains(actual.ToList(), methodName, $"{type.Name} 缺少 {methodName}。");
        }
    }

    private static void AssertPublicProperties(Type type, IReadOnlyCollection<string> propertyNames)
    {
        HashSet<string> actual = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string propertyName in propertyNames)
        {
            CollectionAssert.Contains(actual.ToList(), propertyName, $"{type.Name} 缺少 {propertyName}。");
        }
    }

    private static void AssertJsonArrayContains(JsonElement array, string propertyName, string expectedValue)
    {
        _ = FindJsonArrayItem(array, propertyName, expectedValue);
    }

    private static JsonElement FindJsonArrayItem(JsonElement array, string propertyName, string expectedValue)
    {
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.TryGetProperty(propertyName, out JsonElement value)
                && string.Equals(value.GetString(), expectedValue, StringComparison.Ordinal))
            {
                return item;
            }
        }

        Assert.Fail($"ABI manifest 缺少 {expectedValue}。");
        return default;
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
}
