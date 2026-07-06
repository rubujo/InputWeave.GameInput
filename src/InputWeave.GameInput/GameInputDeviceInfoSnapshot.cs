using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// 不持有原生指標的 GameInput 裝置資訊快照。
/// </summary>
/// <remarks>
/// <see cref="ForceFeedbackMotors"/>、<see cref="InputReports"/>、<see cref="OutputReports"/> 是集合欄位，
/// 值相等性採逐一比較內容，而不是比較複本陣列的參考。<see cref="Native"/> 內僅供診斷的原生指標欄位
/// （<see cref="GameInputDeviceInfo.DisplayName"/>、<see cref="GameInputDeviceInfo.PnpPath"/> 等）不參與相等性比較，
/// 因為這些指標值只在單次列舉呼叫內有效，不代表邏輯內容差異。
/// </remarks>
public readonly record struct GameInputDeviceInfoSnapshot : IEquatable<GameInputDeviceInfoSnapshot>
{
    internal GameInputDeviceInfoSnapshot(
        GameInputDeviceInfo native,
        string? displayName,
        string? pnpPath,
        GameInputKeyboardInfo? keyboard,
        GameInputMouseInfo? mouse,
        GameInputSensorsInfo? sensors,
        GameInputControllerInfoSnapshot? controller,
        GameInputArcadeStickInfo? arcadeStick,
        GameInputFlightStickInfo? flightStick,
        GameInputGamepadInfo? gamepad,
        GameInputRacingWheelInfo? racingWheel,
        IReadOnlyList<GameInputForceFeedbackMotorInfo> forceFeedbackMotors,
        IReadOnlyList<GameInputRawDeviceReportInfo> inputReports,
        IReadOnlyList<GameInputRawDeviceReportInfo> outputReports)
    {
        Native = native;
        DisplayName = displayName;
        PnpPath = pnpPath;
        Keyboard = keyboard;
        Mouse = mouse;
        Sensors = sensors;
        Controller = controller;
        ArcadeStick = arcadeStick;
        FlightStick = flightStick;
        Gamepad = gamepad;
        RacingWheel = racingWheel;
        ForceFeedbackMotors = forceFeedbackMotors;
        InputReports = inputReports;
        OutputReports = outputReports;
    }

    /// <summary>
    /// 原生固定欄位快照；所有指標欄位只供診斷，不應在快照外解參考。
    /// </summary>
    /// <remarks>
    /// 這個結構中的字串與陣列欄位（例如 <see cref="GameInputDeviceInfo.DisplayName"/>、
    /// <see cref="GameInputDeviceInfo.PnpPath"/>）是指向原生記憶體的指標，生命週期綁定於原始裝置的底層 COM
    /// 物件；裝置釋放後解參考會導致未定義行為。一般情境應改用本類別提供的
    /// <see cref="DisplayName"/>、<see cref="PnpPath"/> 等已複製欄位，而非直接讀取 <see cref="Native"/> 的指標欄位。
    /// </remarks>
    public GameInputDeviceInfo Native { get; }

    /// <summary>
    /// USB 或 HID vendor id。
    /// </summary>
    public ushort VendorId
    {
        get
        {
            return Native.VendorId;
        }
    }

    /// <summary>
    /// USB 或 HID product id。
    /// </summary>
    public ushort ProductId
    {
        get
        {
            return Native.ProductId;
        }
    }

    /// <summary>
    /// 裝置修訂版本號。
    /// </summary>
    public ushort RevisionNumber
    {
        get
        {
            return Native.RevisionNumber;
        }
    }

    /// <summary>
    /// 裝置的 HID usage 資訊。
    /// </summary>
    public GameInputUsage Usage
    {
        get
        {
            return Native.Usage;
        }
    }

    /// <summary>
    /// 裝置所屬的 GameInput device family。
    /// </summary>
    public GameInputDeviceFamily DeviceFamily
    {
        get
        {
            return Native.DeviceFamily;
        }
    }

    /// <summary>
    /// 裝置支援的輸入種類集合。
    /// </summary>
    public GameInputKind SupportedInput
    {
        get
        {
            return Native.SupportedInput;
        }
    }

    /// <summary>
    /// 裝置支援的 rumble motor 集合。
    /// </summary>
    public GameInputRumbleMotors SupportedRumbleMotors
    {
        get
        {
            return Native.SupportedRumbleMotors;
        }
    }

    /// <summary>
    /// 裝置支援的 system button 集合。
    /// </summary>
    public GameInputSystemButtons SupportedSystemButtons
    {
        get
        {
            return Native.SupportedSystemButtons;
        }
    }

    /// <summary>
    /// 裝置容器識別碼。
    /// </summary>
    public Guid ContainerId
    {
        get
        {
            return Native.ContainerId;
        }
    }

    /// <summary>
    /// 使用者可讀的裝置顯示名稱。
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// 裝置的 Plug and Play 路徑。
    /// </summary>
    public string? PnpPath { get; }

    /// <summary>
    /// 鍵盤子資訊；裝置不支援鍵盤時為 null。
    /// </summary>
    public GameInputKeyboardInfo? Keyboard { get; }

    /// <summary>
    /// 滑鼠子資訊；裝置不支援滑鼠時為 null。
    /// </summary>
    public GameInputMouseInfo? Mouse { get; }

    /// <summary>
    /// 感測器子資訊；裝置不支援感測器時為 null。
    /// </summary>
    public GameInputSensorsInfo? Sensors { get; }

    /// <summary>
    /// 一般 controller 子資訊；裝置不支援 controller 時為 null。
    /// </summary>
    public GameInputControllerInfoSnapshot? Controller { get; }

    /// <summary>
    /// Arcade stick 子資訊；裝置不支援 arcade stick 時為 null。
    /// </summary>
    public GameInputArcadeStickInfo? ArcadeStick { get; }

    /// <summary>
    /// Flight stick 子資訊；裝置不支援 flight stick 時為 null。
    /// </summary>
    public GameInputFlightStickInfo? FlightStick { get; }

    /// <summary>
    /// Gamepad 子資訊；裝置不支援 gamepad 時為 null。
    /// </summary>
    public GameInputGamepadInfo? Gamepad { get; }

    /// <summary>
    /// Racing wheel 子資訊；裝置不支援 racing wheel 時為 null。
    /// </summary>
    public GameInputRacingWheelInfo? RacingWheel { get; }

    /// <summary>
    /// Force feedback motor 能力資訊快照。
    /// </summary>
    public IReadOnlyList<GameInputForceFeedbackMotorInfo> ForceFeedbackMotors { get; }

    /// <summary>
    /// 裝置可提供的 raw input report 資訊。
    /// </summary>
    public IReadOnlyList<GameInputRawDeviceReportInfo> InputReports { get; }

    /// <summary>
    /// 裝置可接受的 raw output report 資訊。
    /// </summary>
    public IReadOnlyList<GameInputRawDeviceReportInfo> OutputReports { get; }

    /// <summary>
    /// 比較所有欄位的值相等性，其中 <see cref="ForceFeedbackMotors"/>、<see cref="InputReports"/>、
    /// <see cref="OutputReports"/> 採逐一比較內容；<see cref="Native"/> 內僅供診斷的原生指標欄位不參與比較。
    /// </summary>
    /// <param name="other">要比較的另一個快照。</param>
    /// <returns>兩個快照的所有欄位皆相同時，傳回 true。</returns>
    public bool Equals(GameInputDeviceInfoSnapshot other)
    {
        return VendorId == other.VendorId
            && ProductId == other.ProductId
            && RevisionNumber == other.RevisionNumber
            && Usage.Equals(other.Usage)
            && Native.HardwareVersion.Equals(other.Native.HardwareVersion)
            && Native.FirmwareVersion.Equals(other.Native.FirmwareVersion)
            && Native.DeviceId.Equals(other.Native.DeviceId)
            && Native.DeviceRootId.Equals(other.Native.DeviceRootId)
            && DeviceFamily == other.DeviceFamily
            && SupportedInput == other.SupportedInput
            && SupportedRumbleMotors == other.SupportedRumbleMotors
            && SupportedSystemButtons == other.SupportedSystemButtons
            && ContainerId == other.ContainerId
            && DisplayName == other.DisplayName
            && PnpPath == other.PnpPath
            && EqualityComparer<GameInputKeyboardInfo?>.Default.Equals(Keyboard, other.Keyboard)
            && EqualityComparer<GameInputMouseInfo?>.Default.Equals(Mouse, other.Mouse)
            && EqualityComparer<GameInputSensorsInfo?>.Default.Equals(Sensors, other.Sensors)
            && EqualityComparer<GameInputControllerInfoSnapshot?>.Default.Equals(Controller, other.Controller)
            && EqualityComparer<GameInputArcadeStickInfo?>.Default.Equals(ArcadeStick, other.ArcadeStick)
            && EqualityComparer<GameInputFlightStickInfo?>.Default.Equals(FlightStick, other.FlightStick)
            && EqualityComparer<GameInputGamepadInfo?>.Default.Equals(Gamepad, other.Gamepad)
            && EqualityComparer<GameInputRacingWheelInfo?>.Default.Equals(RacingWheel, other.RacingWheel)
            && ForceFeedbackMotors.SequenceEqual(other.ForceFeedbackMotors)
            && InputReports.SequenceEqual(other.InputReports)
            && OutputReports.SequenceEqual(other.OutputReports);
    }

    /// <summary>
    /// 依所有欄位內容計算雜湊碼；<see cref="Native"/> 內僅供診斷的原生指標欄位不參與計算。
    /// </summary>
    /// <returns>雜湊碼。</returns>
    public override int GetHashCode()
    {
        int hash = VendorId.GetHashCode();
        hash = HashCodeCombiner.Combine(hash, ProductId.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, RevisionNumber.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, Usage.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, Native.HardwareVersion.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, Native.FirmwareVersion.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, Native.DeviceId.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, Native.DeviceRootId.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, DeviceFamily.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, SupportedInput.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, SupportedRumbleMotors.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, SupportedSystemButtons.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, ContainerId.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, DisplayName?.GetHashCode() ?? 0);
        hash = HashCodeCombiner.Combine(hash, PnpPath?.GetHashCode() ?? 0);
        hash = HashCodeCombiner.Combine(hash, Keyboard.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, Mouse.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, Sensors.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, Controller.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, ArcadeStick.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, FlightStick.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, Gamepad.GetHashCode());
        hash = HashCodeCombiner.Combine(hash, RacingWheel.GetHashCode());
        hash = HashCodeCombiner.CombineRange(hash, ForceFeedbackMotors);
        hash = HashCodeCombiner.CombineRange(hash, InputReports);
        hash = HashCodeCombiner.CombineRange(hash, OutputReports);
        return hash;
    }

    internal static GameInputDeviceInfoSnapshot FromNative(GameInputDeviceInfo native)
    {
        return new GameInputDeviceInfoSnapshot(
            native,
            NativeUtf8String.FromNullTerminated(native.DisplayName),
            NativeUtf8String.FromNullTerminated(native.PnpPath),
            PtrToNullableStructure<GameInputKeyboardInfo>(native.KeyboardInfo),
            PtrToNullableStructure<GameInputMouseInfo>(native.MouseInfo),
            PtrToNullableStructure<GameInputSensorsInfo>(native.SensorsInfo),
            GameInputControllerInfoSnapshot.FromPointer(native.ControllerInfo),
            PtrToNullableStructure<GameInputArcadeStickInfo>(native.ArcadeStickInfo),
            PtrToNullableStructure<GameInputFlightStickInfo>(native.FlightStickInfo),
            PtrToNullableStructure<GameInputGamepadInfo>(native.GamepadInfo),
            PtrToNullableStructure<GameInputRacingWheelInfo>(native.RacingWheelInfo),
            ReadArray<GameInputForceFeedbackMotorInfo>(native.ForceFeedbackMotorInfo, native.ForceFeedbackMotorCount),
            ReadArray<GameInputRawDeviceReportInfo>(native.InputReportInfo, native.InputReportCount),
            ReadArray<GameInputRawDeviceReportInfo>(native.OutputReportInfo, native.OutputReportCount));
    }

#if NET10_0_OR_GREATER
    private static T? PtrToNullableStructure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(IntPtr pointer)
        where T : struct
#else
    private static T? PtrToNullableStructure<T>(IntPtr pointer)
        where T : struct
#endif
    {
        return pointer == IntPtr.Zero ? null : Marshal.PtrToStructure<T>(pointer);
    }

#if NET10_0_OR_GREATER
    internal static IReadOnlyList<T> ReadArray<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(IntPtr pointer, uint count)
        where T : struct
#else
    internal static IReadOnlyList<T> ReadArray<T>(IntPtr pointer, uint count)
        where T : struct
#endif
    {
        if (pointer == IntPtr.Zero || count == 0)
        {
            return Array.Empty<T>();
        }

        Type type = typeof(T);
        bool isEnum = type.IsEnum;
        TypeCode underlyingTypeCode = isEnum ? Type.GetTypeCode(Enum.GetUnderlyingType(type)) : TypeCode.Empty;
        int itemSize = isEnum ? GetUnderlyingTypeSize(underlyingTypeCode) : Marshal.SizeOf<T>();

        T[] values = new T[checked((int)count)];
        for (int index = 0; index < values.Length; index++)
        {
            IntPtr elementPointer = IntPtr.Add(pointer, checked(index * itemSize));
            values[index] = isEnum
                ? (T)Enum.ToObject(type, ReadUnderlyingEnumValue(elementPointer, underlyingTypeCode))
                : Marshal.PtrToStructure<T>(elementPointer);
        }

        return values;
    }

    private static int GetUnderlyingTypeSize(TypeCode typeCode)
    {
        return typeCode switch
        {
            TypeCode.Byte or TypeCode.SByte => sizeof(byte),
            TypeCode.Int16 or TypeCode.UInt16 => sizeof(short),
            TypeCode.Int32 or TypeCode.UInt32 => sizeof(int),
            TypeCode.Int64 or TypeCode.UInt64 => sizeof(long),
            _ => throw new NotSupportedException($"不支援的 enum underlying type：{typeCode}。")
        };
    }

    private static object ReadUnderlyingEnumValue(IntPtr pointer, TypeCode underlyingTypeCode)
    {
        return underlyingTypeCode switch
        {
            TypeCode.Byte => Marshal.ReadByte(pointer),
            TypeCode.SByte => unchecked((sbyte)Marshal.ReadByte(pointer)),
            TypeCode.Int16 => unchecked((short)Marshal.ReadInt16(pointer)),
            TypeCode.UInt16 => unchecked((ushort)Marshal.ReadInt16(pointer)),
            TypeCode.Int32 => Marshal.ReadInt32(pointer),
            TypeCode.UInt32 => unchecked((uint)Marshal.ReadInt32(pointer)),
            TypeCode.Int64 => Marshal.ReadInt64(pointer),
            TypeCode.UInt64 => unchecked((ulong)Marshal.ReadInt64(pointer)),
            _ => throw new NotSupportedException($"不支援的 enum underlying type：{underlyingTypeCode}。")
        };
    }
}

/// <summary>
/// 不持有原生指標的 controller 子資訊快照。
/// </summary>
/// <remarks>
/// <see cref="AxisLabels"/>、<see cref="ButtonLabels"/>、<see cref="Switches"/> 是集合欄位，
/// 值相等性採逐一比較內容，而不是比較複本陣列的參考。<see cref="Native"/> 完全不參與相等性比較——
/// 它只有 Count 欄位與僅供診斷的原生指標欄位，Count 已完整反映在上述集合欄位的長度中。
/// </remarks>
public readonly record struct GameInputControllerInfoSnapshot : IEquatable<GameInputControllerInfoSnapshot>
{
    internal GameInputControllerInfoSnapshot(
        GameInputControllerInfo native,
        IReadOnlyList<GameInputLabel> axisLabels,
        IReadOnlyList<GameInputLabel> buttonLabels,
        IReadOnlyList<GameInputControllerSwitchInfoSnapshot> switches)
    {
        Native = native;
        AxisLabels = axisLabels;
        ButtonLabels = buttonLabels;
        Switches = switches;
    }

    /// <summary>
    /// 原生 controller 固定欄位快照；指標欄位只供診斷。
    /// </summary>
    /// <remarks>
    /// 這個結構中的陣列欄位（例如 <see cref="GameInputControllerInfo.ControllerAxisLabels"/>）是指向原生記憶體的指標，
    /// 生命週期綁定於原始裝置的底層 COM 物件；裝置釋放後解參考會導致未定義行為。一般情境應改用本類別提供的
    /// <see cref="AxisLabels"/>、<see cref="ButtonLabels"/>、<see cref="Switches"/> 等已複製欄位。
    /// </remarks>
    public GameInputControllerInfo Native { get; }

    /// <summary>
    /// Controller axis label 清單。
    /// </summary>
    public IReadOnlyList<GameInputLabel> AxisLabels { get; }

    /// <summary>
    /// Controller button label 清單。
    /// </summary>
    public IReadOnlyList<GameInputLabel> ButtonLabels { get; }

    /// <summary>
    /// Controller switch 資訊快照清單。
    /// </summary>
    public IReadOnlyList<GameInputControllerSwitchInfoSnapshot> Switches { get; }

    /// <summary>
    /// 逐一比較 <see cref="AxisLabels"/>、<see cref="ButtonLabels"/>、<see cref="Switches"/> 內容的值相等性；
    /// <see cref="Native"/> 內僅供診斷的原生指標欄位不參與比較（其 Count 欄位已完整反映在各集合欄位長度中）。
    /// </summary>
    /// <param name="other">要比較的另一個快照。</param>
    /// <returns>兩個快照的所有欄位皆相同時，傳回 true。</returns>
    public bool Equals(GameInputControllerInfoSnapshot other)
    {
        return AxisLabels.SequenceEqual(other.AxisLabels)
            && ButtonLabels.SequenceEqual(other.ButtonLabels)
            && Switches.SequenceEqual(other.Switches);
    }

    /// <summary>
    /// 依 <see cref="AxisLabels"/>、<see cref="ButtonLabels"/>、<see cref="Switches"/> 內容計算雜湊碼。
    /// </summary>
    /// <returns>雜湊碼。</returns>
    public override int GetHashCode()
    {
        int hash = 17;
        hash = HashCodeCombiner.CombineRange(hash, AxisLabels);
        hash = HashCodeCombiner.CombineRange(hash, ButtonLabels);
        hash = HashCodeCombiner.CombineRange(hash, Switches);
        return hash;
    }

    internal static GameInputControllerInfoSnapshot? FromPointer(IntPtr pointer)
    {
        if (pointer == IntPtr.Zero)
        {
            return null;
        }

        GameInputControllerInfo native = Marshal.PtrToStructure<GameInputControllerInfo>(pointer);
        return new GameInputControllerInfoSnapshot(
            native,
            GameInputDeviceInfoSnapshot.ReadArray<GameInputLabel>(native.ControllerAxisLabels, native.ControllerAxisCount),
            GameInputDeviceInfoSnapshot.ReadArray<GameInputLabel>(native.ControllerButtonLabels, native.ControllerButtonCount),
            ReadSwitchInfo(native.ControllerSwitchInfo, native.ControllerSwitchCount));
    }

    private static GameInputControllerSwitchInfoSnapshot[] ReadSwitchInfo(IntPtr pointer, uint count)
    {
        if (pointer == IntPtr.Zero || count == 0)
        {
            return [];
        }

        int itemSize = Marshal.SizeOf<GameInputControllerSwitchInfo>();
        GameInputControllerSwitchInfoSnapshot[] values = new GameInputControllerSwitchInfoSnapshot[checked((int)count)];
        for (int index = 0; index < values.Length; index++)
        {
            GameInputControllerSwitchInfo native = Marshal.PtrToStructure<GameInputControllerSwitchInfo>(IntPtr.Add(pointer, checked(index * itemSize)));
            values[index] = GameInputControllerSwitchInfoSnapshot.FromNative(native);
        }

        return values;
    }
}

/// <summary>
/// 不持有 fixed buffer 的 controller switch 子資訊快照。
/// </summary>
/// <remarks>
/// <see cref="Labels"/> 是集合欄位，值相等性採逐一比較內容，而不是比較複本陣列的參考。
/// </remarks>
public readonly record struct GameInputControllerSwitchInfoSnapshot : IEquatable<GameInputControllerSwitchInfoSnapshot>
{
    internal GameInputControllerSwitchInfoSnapshot(IReadOnlyList<GameInputLabel> labels, GameInputSwitchKind kind)
    {
        Labels = labels;
        Kind = kind;
    }

    /// <summary>
    /// Switch 每個位置對應的 label 清單。
    /// </summary>
    public IReadOnlyList<GameInputLabel> Labels { get; }

    /// <summary>
    /// Switch 的原生種類。
    /// </summary>
    public GameInputSwitchKind Kind { get; }

    /// <summary>
    /// 逐一比較 <see cref="Labels"/> 內容的值相等性。
    /// </summary>
    /// <param name="other">要比較的另一個快照。</param>
    /// <returns>兩個快照的 <see cref="Kind"/> 與 <see cref="Labels"/> 內容皆相同時，傳回 true。</returns>
    public bool Equals(GameInputControllerSwitchInfoSnapshot other)
    {
        return Kind == other.Kind && Labels.SequenceEqual(other.Labels);
    }

    /// <summary>
    /// 依 <see cref="Kind"/> 與 <see cref="Labels"/> 內容計算雜湊碼。
    /// </summary>
    /// <returns>雜湊碼。</returns>
    public override int GetHashCode()
    {
        return HashCodeCombiner.CombineRange(Kind.GetHashCode(), Labels);
    }

    internal static unsafe GameInputControllerSwitchInfoSnapshot FromNative(GameInputControllerSwitchInfo native)
    {
        GameInputLabel[] labels = new GameInputLabel[GameInputConstants.MaxSwitchStates];
        for (int index = 0; index < labels.Length; index++)
        {
            labels[index] = (GameInputLabel)native.Labels[index];
        }

        return new GameInputControllerSwitchInfoSnapshot(labels, native.Kind);
    }
}

/// <summary>
/// 不持有原生 haptic 緩衝區的觸覺資訊快照。
/// </summary>
/// <remarks>
/// <see cref="Locations"/> 是集合欄位，值相等性採逐一比較內容，而不是比較複本陣列的參考。
/// </remarks>
public readonly record struct GameInputHapticInfoSnapshot : IEquatable<GameInputHapticInfoSnapshot>
{
    internal GameInputHapticInfoSnapshot(string audioEndpointId, IReadOnlyList<Guid> locations)
    {
        AudioEndpointId = audioEndpointId;
        Locations = locations;
    }

    /// <summary>
    /// Haptic 音訊端點識別碼。
    /// </summary>
    public string AudioEndpointId { get; }

    /// <summary>
    /// Haptic 位置識別碼清單。
    /// </summary>
    public IReadOnlyList<Guid> Locations { get; }

    /// <summary>
    /// 逐一比較 <see cref="Locations"/> 內容的值相等性。
    /// </summary>
    /// <param name="other">要比較的另一個快照。</param>
    /// <returns>兩個快照的 <see cref="AudioEndpointId"/> 與 <see cref="Locations"/> 內容皆相同時，傳回 true。</returns>
    public bool Equals(GameInputHapticInfoSnapshot other)
    {
        return AudioEndpointId == other.AudioEndpointId && Locations.SequenceEqual(other.Locations);
    }

    /// <summary>
    /// 依 <see cref="AudioEndpointId"/> 與 <see cref="Locations"/> 內容計算雜湊碼。
    /// </summary>
    /// <returns>雜湊碼。</returns>
    public override int GetHashCode()
    {
        return HashCodeCombiner.CombineRange(AudioEndpointId.GetHashCode(), Locations);
    }

    internal static GameInputHapticInfoSnapshot FromNative(GameInputHapticInfo native)
    {
        Guid[] locations = [];
        if (native.Locations is not null && native.LocationCount > 0)
        {
            int count = Math.Min(checked((int)native.LocationCount), native.Locations.Length);
            locations = new Guid[count];
            Array.Copy(native.Locations, locations, count);
        }

        return new GameInputHapticInfoSnapshot(native.AudioEndpointId ?? string.Empty, locations);
    }
}
