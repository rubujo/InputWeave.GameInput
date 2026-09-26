using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputSnapshotEqualityTests
{
    [TestMethod]
    public void DefaultSnapshotsWithCollectionsAreSafeToUse()
    {
        // TryGet* 失敗時會輸出 default 快照；集合欄位不得為 null，比較、雜湊與讀取都不能拋出例外。
        AssertDefaultIsSafe<KeyboardReadingSnapshot>(static snapshot => [snapshot.Keys]);
        AssertDefaultIsSafe<ControllerReadingSnapshot>(static snapshot => [snapshot.Axes, snapshot.Buttons, snapshot.Switches]);
        AssertDefaultIsSafe<RawDeviceReportSnapshot>(static snapshot => [snapshot.Data, snapshot.GetData()]);
        AssertDefaultIsSafe<GameInputDeviceInfoSnapshot>(static snapshot => [snapshot.ForceFeedbackMotors, snapshot.InputReports, snapshot.OutputReports]);
        AssertDefaultIsSafe<GameInputControllerInfoSnapshot>(static snapshot => [snapshot.AxisLabels, snapshot.ButtonLabels, snapshot.Switches]);
        AssertDefaultIsSafe<GameInputControllerSwitchInfoSnapshot>(static snapshot => [snapshot.Labels]);
        AssertDefaultIsSafe<GameInputHapticInfoSnapshot>(static snapshot => [snapshot.Locations]);
        Assert.IsEmpty(default(GameInputRuntimeProbeInfo).Candidates);
        Assert.IsNotNull(default(GameInputHapticInfoSnapshot).AudioEndpointId);
        Assert.IsNotNull(default(GameInputRuntimeProbeInfo).LoaderPolicy);
        Assert.IsNotNull(default(GameInputRuntimeProbeInfo).SelectedModulePath);
        Assert.IsNotNull(default(GameInputRuntimeInfo).LoaderPolicy);
        Assert.IsNotNull(default(GameInputRuntimeInfo).LoadedModulePath);
        Assert.IsNotNull(default(GameInputRuntimeCandidateInfo).ModulePath);
    }

    private static void AssertDefaultIsSafe<T>(Func<T, System.Collections.IEnumerable[]> collections)
        where T : struct, IEquatable<T>
    {
        T value = default;
        Assert.IsTrue(value.Equals(default), $"{typeof(T).Name} 的 default 值應等於自己。");
        _ = value.GetHashCode();
        _ = value.ToString();
        foreach (System.Collections.IEnumerable collection in collections(value))
        {
            Assert.IsNotNull(collection, $"{typeof(T).Name} 的集合欄位不得為 null。");
            Assert.IsFalse(collection.GetEnumerator().MoveNext(), $"{typeof(T).Name} 的 default 集合應為空。");
        }
    }
    [TestMethod]
    public void GamepadReadingSnapshotsWithSameValuesAreEqual()
    {
        GameInputGamepadState state = new()
        {
            Buttons = GameInputGamepadButtons.GameInputGamepadA,
            LeftTrigger = 0.5f
        };

        GamepadReadingSnapshot first = new(42, state);
        GamepadReadingSnapshot second = new(42, state);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void GamepadReadingSnapshotsWithDifferentValuesAreNotEqual()
    {
        GameInputGamepadState state = new() { Buttons = GameInputGamepadButtons.GameInputGamepadA };

        GamepadReadingSnapshot first = new(1, state);
        GamepadReadingSnapshot second = new(2, state);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void KeyboardReadingSnapshotsWithEqualKeysContentAreEqual()
    {
        GameInputKeyState[] keys = [new GameInputKeyState { ScanCode = 10, VirtualKey = 65 }];

        KeyboardReadingSnapshot first = new(1, keys);
        KeyboardReadingSnapshot second = new(1, keys);

        Assert.AreNotSame(first.Keys, second.Keys, "每次建立快照都會複製陣列，Keys 應是不同的複本陣列。");
        Assert.AreEqual(first, second, "Keys 改為逐一比較內容，內容相同的不同複本陣列應視為相等。");
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void KeyboardReadingSnapshotsWithDifferentKeysContentAreNotEqual()
    {
        KeyboardReadingSnapshot first = new(1, [new GameInputKeyState { ScanCode = 10, VirtualKey = 65 }]);
        KeyboardReadingSnapshot second = new(1, [new GameInputKeyState { ScanCode = 11, VirtualKey = 66 }]);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void ControllerReadingSnapshotsWithEqualContentAreEqual()
    {
        ControllerReadingSnapshot first = new(1, [0.5f], [true], [GameInputSwitchPosition.GameInputSwitchCenter]);
        ControllerReadingSnapshot second = new(1, [0.5f], [true], [GameInputSwitchPosition.GameInputSwitchCenter]);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void ControllerReadingSnapshotsWithDifferentAxesAreNotEqual()
    {
        ControllerReadingSnapshot first = new(1, [0.5f], [true], [GameInputSwitchPosition.GameInputSwitchCenter]);
        ControllerReadingSnapshot second = new(1, [0.75f], [true], [GameInputSwitchPosition.GameInputSwitchCenter]);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void RawDeviceReportSnapshotsWithEqualDataAreEqual()
    {
        GameInputRawDeviceReportInfo info = new() { Id = 7 };

        RawDeviceReportSnapshot first = new(1, info, [1, 2, 3]);
        RawDeviceReportSnapshot second = new(1, info, [1, 2, 3]);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void RawDeviceReportSnapshotsWithDifferentDataAreNotEqual()
    {
        GameInputRawDeviceReportInfo info = new() { Id = 7 };

        RawDeviceReportSnapshot first = new(1, info, [1, 2, 3]);
        RawDeviceReportSnapshot second = new(1, info, [1, 2, 4]);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void ControllerInfoSnapshotsWithDifferentNativePointersButEqualContentAreEqual()
    {
        GameInputControllerInfo nativeFirst = new() { ControllerAxisCount = 1, ControllerAxisLabels = (IntPtr)0x1111 };
        GameInputControllerInfo nativeSecond = new() { ControllerAxisCount = 1, ControllerAxisLabels = (IntPtr)0x2222 };

        GameInputControllerInfoSnapshot first = new(nativeFirst, [GameInputLabel.GameInputLabelUnknown], [], []);
        GameInputControllerInfoSnapshot second = new(nativeSecond, [GameInputLabel.GameInputLabelUnknown], [], []);

        Assert.AreEqual(first, second, "Native 只保留原生指標與 Count，指標值不同不應影響已投影內容的相等性。");
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void ControllerInfoSnapshotsWithDifferentAxisLabelsAreNotEqual()
    {
        GameInputControllerInfo native = new() { ControllerAxisCount = 1, ControllerAxisLabels = (IntPtr)0x1111 };

        GameInputControllerInfoSnapshot first = new(native, [GameInputLabel.GameInputLabelUnknown], [], []);
        GameInputControllerInfoSnapshot second = new(native, [GameInputLabel.GameInputLabelNone], [], []);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void DeviceInfoSnapshotsWithDifferentNativePointersButEqualContentAreEqual()
    {
        GameInputDeviceInfoSnapshot first = CreateDeviceInfoSnapshot(vendorId: 1, displayNamePointer: (IntPtr)0x1111);
        GameInputDeviceInfoSnapshot second = CreateDeviceInfoSnapshot(vendorId: 1, displayNamePointer: (IntPtr)0x2222);

        Assert.AreEqual(first, second, "Native 的原生指標欄位不應參與相等性比較。");
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void DeviceInfoSnapshotsWithDifferentVendorIdAreNotEqual()
    {
        GameInputDeviceInfoSnapshot first = CreateDeviceInfoSnapshot(vendorId: 1, displayNamePointer: (IntPtr)0x1111);
        GameInputDeviceInfoSnapshot second = CreateDeviceInfoSnapshot(vendorId: 2, displayNamePointer: (IntPtr)0x1111);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void ControllerSwitchInfoSnapshotsWithEqualLabelsAreEqual()
    {
        GameInputControllerSwitchInfoSnapshot first = new([GameInputLabel.GameInputLabelUnknown], GameInputSwitchKind.GameInputUnknownSwitchKind);
        GameInputControllerSwitchInfoSnapshot second = new([GameInputLabel.GameInputLabelUnknown], GameInputSwitchKind.GameInputUnknownSwitchKind);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void ControllerSwitchInfoSnapshotsWithDifferentLabelsAreNotEqual()
    {
        GameInputControllerSwitchInfoSnapshot first = new([GameInputLabel.GameInputLabelUnknown], GameInputSwitchKind.GameInputUnknownSwitchKind);
        GameInputControllerSwitchInfoSnapshot second = new([GameInputLabel.GameInputLabelNone], GameInputSwitchKind.GameInputUnknownSwitchKind);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void HapticInfoSnapshotsWithEqualLocationsAreEqual()
    {
        Guid location = Guid.NewGuid();
        GameInputHapticInfoSnapshot first = new("端點", [location]);
        GameInputHapticInfoSnapshot second = new("端點", [location]);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void HapticInfoSnapshotsWithDifferentLocationsAreNotEqual()
    {
        GameInputHapticInfoSnapshot first = new("端點", [Guid.NewGuid()]);
        GameInputHapticInfoSnapshot second = new("端點", [Guid.NewGuid()]);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void MouseReadingSnapshotsWithSameValuesAreEqual()
    {
        GameInputMouseState state = new() { Buttons = GameInputMouseButtons.GameInputMouseLeftButton };

        MouseReadingSnapshot first = new(1, state);
        MouseReadingSnapshot second = new(1, state);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void MouseReadingSnapshotsWithDifferentTimestampAreNotEqual()
    {
        GameInputMouseState state = new() { Buttons = GameInputMouseButtons.GameInputMouseLeftButton };

        MouseReadingSnapshot first = new(1, state);
        MouseReadingSnapshot second = new(2, state);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void SensorsReadingSnapshotsWithSameValuesAreEqual()
    {
        GameInputSensorsState state = new() { AccelerationInGX = 1.5f };

        SensorsReadingSnapshot first = new(1, state);
        SensorsReadingSnapshot second = new(1, state);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void SensorsReadingSnapshotsWithDifferentTimestampAreNotEqual()
    {
        GameInputSensorsState state = new() { AccelerationInGX = 1.5f };

        SensorsReadingSnapshot first = new(1, state);
        SensorsReadingSnapshot second = new(2, state);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void ArcadeStickReadingSnapshotsWithSameValuesAreEqual()
    {
        GameInputArcadeStickState state = new() { Buttons = GameInputArcadeStickButtons.GameInputArcadeStickMenu };

        ArcadeStickReadingSnapshot first = new(1, state);
        ArcadeStickReadingSnapshot second = new(1, state);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void ArcadeStickReadingSnapshotsWithDifferentTimestampAreNotEqual()
    {
        GameInputArcadeStickState state = new() { Buttons = GameInputArcadeStickButtons.GameInputArcadeStickMenu };

        ArcadeStickReadingSnapshot first = new(1, state);
        ArcadeStickReadingSnapshot second = new(2, state);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void FlightStickReadingSnapshotsWithSameValuesAreEqual()
    {
        GameInputFlightStickState state = new() { Buttons = GameInputFlightStickButtons.GameInputFlightStickMenu };

        FlightStickReadingSnapshot first = new(1, state);
        FlightStickReadingSnapshot second = new(1, state);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void FlightStickReadingSnapshotsWithDifferentTimestampAreNotEqual()
    {
        GameInputFlightStickState state = new() { Buttons = GameInputFlightStickButtons.GameInputFlightStickMenu };

        FlightStickReadingSnapshot first = new(1, state);
        FlightStickReadingSnapshot second = new(2, state);

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void RacingWheelReadingSnapshotsWithSameValuesAreEqual()
    {
        GameInputRacingWheelState state = new() { Buttons = GameInputRacingWheelButtons.GameInputRacingWheelMenu };

        RacingWheelReadingSnapshot first = new(1, state);
        RacingWheelReadingSnapshot second = new(1, state);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [TestMethod]
    public void RacingWheelReadingSnapshotsWithDifferentTimestampAreNotEqual()
    {
        GameInputRacingWheelState state = new() { Buttons = GameInputRacingWheelButtons.GameInputRacingWheelMenu };

        RacingWheelReadingSnapshot first = new(1, state);
        RacingWheelReadingSnapshot second = new(2, state);

        Assert.AreNotEqual(first, second);
    }

    private static GameInputDeviceInfoSnapshot CreateDeviceInfoSnapshot(ushort vendorId, IntPtr displayNamePointer)
    {
        GameInputDeviceInfo native = new()
        {
            VendorId = vendorId,
            DisplayName = displayNamePointer,
            PnpPath = displayNamePointer
        };

        return new GameInputDeviceInfoSnapshot(
            native,
            "顯示名稱",
            "PnP 路徑",
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
