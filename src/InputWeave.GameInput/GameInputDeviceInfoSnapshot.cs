using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// 不持有原生指標的 GameInput 裝置資訊快照。
/// </summary>
public sealed class GameInputDeviceInfoSnapshot
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

    private static T? PtrToNullableStructure<T>(IntPtr pointer)
        where T : struct
    {
        return pointer == IntPtr.Zero ? null : Marshal.PtrToStructure<T>(pointer);
    }

    internal static IReadOnlyList<T> ReadArray<T>(IntPtr pointer, uint count)
        where T : struct
    {
        if (pointer == IntPtr.Zero || count == 0)
        {
            return Array.Empty<T>();
        }

        int itemSize = SizeOf<T>();
        T[] values = new T[checked((int)count)];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = ReadElement<T>(IntPtr.Add(pointer, checked(index * itemSize)));
        }

        return values;
    }

    private static int SizeOf<T>()
        where T : struct
    {
        Type type = typeof(T);
        return type.IsEnum
            ? Marshal.SizeOf(Enum.GetUnderlyingType(type))
            : Marshal.SizeOf<T>();
    }

    private static T ReadElement<T>(IntPtr pointer)
        where T : struct
    {
        Type type = typeof(T);
        if (!type.IsEnum)
        {
            return Marshal.PtrToStructure<T>(pointer);
        }

        Type underlyingType = Enum.GetUnderlyingType(type);
        object value = Type.GetTypeCode(underlyingType) switch
        {
            TypeCode.Byte => Marshal.ReadByte(pointer),
            TypeCode.SByte => unchecked((sbyte)Marshal.ReadByte(pointer)),
            TypeCode.Int16 => unchecked((short)Marshal.ReadInt16(pointer)),
            TypeCode.UInt16 => unchecked((ushort)Marshal.ReadInt16(pointer)),
            TypeCode.Int32 => Marshal.ReadInt32(pointer),
            TypeCode.UInt32 => unchecked((uint)Marshal.ReadInt32(pointer)),
            TypeCode.Int64 => Marshal.ReadInt64(pointer),
            TypeCode.UInt64 => unchecked((ulong)Marshal.ReadInt64(pointer)),
            _ => throw new NotSupportedException($"不支援的 enum underlying type：{underlyingType.FullName}。")
        };

        return (T)Enum.ToObject(type, value);
    }
}

/// <summary>
/// 不持有原生指標的 controller 子資訊快照。
/// </summary>
public sealed class GameInputControllerInfoSnapshot
{
    private GameInputControllerInfoSnapshot(
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
public sealed class GameInputControllerSwitchInfoSnapshot
{
    private GameInputControllerSwitchInfoSnapshot(IReadOnlyList<GameInputLabel> labels, GameInputSwitchKind kind)
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
public sealed class GameInputHapticInfoSnapshot
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
