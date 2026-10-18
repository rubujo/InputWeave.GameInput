using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>An independent API 4 device information snapshot. 獨立的 API 4 裝置資訊快照。</summary>
public readonly record struct GameInputDeviceInfoSnapshot : IEquatable<GameInputDeviceInfoSnapshot>
{
    internal GameInputDeviceInfoSnapshot(
        GameInputDeviceIdentity native,
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
        Identity = native;
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

    /// <summary>The copied Identity data. 已複製的 Identity 資料。</summary>
    public GameInputDeviceIdentity Identity { get; }

    /// <summary>The copied DisplayName data. 已複製的 DisplayName 資料。</summary>
    public string? DisplayName { get; }

    /// <summary>The copied PnpPath data. 已複製的 PnpPath 資料。</summary>
    public string? PnpPath { get; }

    /// <summary>The copied Keyboard data. 已複製的 Keyboard 資料。</summary>
    public GameInputKeyboardInfo? Keyboard { get; }

    /// <summary>The copied Mouse data. 已複製的 Mouse 資料。</summary>
    public GameInputMouseInfo? Mouse { get; }

    /// <summary>The copied Sensors data. 已複製的 Sensors 資料。</summary>
    public GameInputSensorsInfo? Sensors { get; }

    /// <summary>The copied Controller data. 已複製的 Controller 資料。</summary>
    public GameInputControllerInfoSnapshot? Controller { get; }

    /// <summary>The copied ArcadeStick data. 已複製的 ArcadeStick 資料。</summary>
    public GameInputArcadeStickInfo? ArcadeStick { get; }

    /// <summary>The copied FlightStick data. 已複製的 FlightStick 資料。</summary>
    public GameInputFlightStickInfo? FlightStick { get; }

    /// <summary>The copied Gamepad data. 已複製的 Gamepad 資料。</summary>
    public GameInputGamepadInfo? Gamepad { get; }

    /// <summary>The copied RacingWheel data. 已複製的 RacingWheel 資料。</summary>
    public GameInputRacingWheelInfo? RacingWheel { get; }

    /// <summary>The copied ForceFeedbackMotors data. 已複製的 ForceFeedbackMotors 資料。</summary>
    public IReadOnlyList<GameInputForceFeedbackMotorInfo> ForceFeedbackMotors { get => field ?? Array.Empty<GameInputForceFeedbackMotorInfo>(); }

    /// <summary>The copied InputReports data. 已複製的 InputReports 資料。</summary>
    public IReadOnlyList<GameInputRawDeviceReportInfo> InputReports { get => field ?? Array.Empty<GameInputRawDeviceReportInfo>(); }

    /// <summary>The copied OutputReports data. 已複製的 OutputReports 資料。</summary>
    public IReadOnlyList<GameInputRawDeviceReportInfo> OutputReports { get => field ?? Array.Empty<GameInputRawDeviceReportInfo>(); }

    /// <summary>The VendorId value. 供應商識別碼。</summary>
    public ushort VendorId => Identity.VendorId;

    /// <summary>The ProductId value. 產品識別碼。</summary>
    public ushort ProductId => Identity.ProductId;

    /// <summary>The RevisionNumber value. 修訂編號。</summary>
    public ushort RevisionNumber => Identity.RevisionNumber;

    /// <summary>The Usage value. HID 使用方式。</summary>
    public GameInputUsage Usage => Identity.Usage;

    /// <summary>The HardwareVersion value. 硬體版本。</summary>
    public GameInputVersion HardwareVersion => Identity.HardwareVersion;

    /// <summary>The FirmwareVersion value. 韌體版本。</summary>
    public GameInputVersion FirmwareVersion => Identity.FirmwareVersion;

    /// <summary>The DeviceId value. 裝置識別碼。</summary>
    public AppLocalDeviceId DeviceId => Identity.DeviceId;

    /// <summary>The DeviceRootId value. 根裝置識別碼。</summary>
    public AppLocalDeviceId DeviceRootId => Identity.DeviceRootId;

    /// <summary>The DeviceFamily value. 裝置家族。</summary>
    public GameInputDeviceFamily DeviceFamily => Identity.DeviceFamily;

    /// <summary>The SupportedInput value. 支援的輸入種類。</summary>
    public GameInputKind SupportedInput => Identity.SupportedInput;

    /// <summary>The SupportedRumbleMotors value. 支援的震動馬達。</summary>
    public GameInputRumbleMotors SupportedRumbleMotors => Identity.SupportedRumbleMotors;

    /// <summary>The SupportedSystemButtons value. 支援的系統按鈕。</summary>
    public GameInputSystemButtons SupportedSystemButtons => Identity.SupportedSystemButtons;

    /// <summary>The ContainerId value. 容器識別碼。</summary>
    public Guid ContainerId => Identity.ContainerId;

    /// <summary>The copied SupportedLightingGroups data. 已複製的 SupportedLightingGroups 資料。</summary>
    public GameInputLightingGroups SupportedLightingGroups { get; init; }

    /// <summary>The copied XInputDeviceIndex data. 已複製的 XInputDeviceIndex 資料。</summary>
    public uint? XInputDeviceIndex { get; init; }

    /// <summary>The copied FeatureReports data. 已複製的 FeatureReports 資料。</summary>
    public IReadOnlyList<GameInputRawDeviceReportInfo> FeatureReports { get => field ?? Array.Empty<GameInputRawDeviceReportInfo>(); init; }

    /// <summary>The copied TouchSurfaces data. 已複製的 TouchSurfaces 資料。</summary>
    public IReadOnlyList<GameInputTouchSurfaceInfo> TouchSurfaces { get => field ?? Array.Empty<GameInputTouchSurfaceInfo>(); init; }

    /// <summary>Compares copied values and collection contents. 比較已複製的值與集合內容。</summary>
    /// <param name="other">The other snapshot. 另一個快照。</param>
    /// <returns>True when all contents are equal. 所有內容相等時為 true。</returns>
    public bool Equals(GameInputDeviceInfoSnapshot other)
    {
        return EqualityComparer<GameInputDeviceIdentity>.Default.Equals(Identity, other.Identity)
            && EqualityComparer<string?>.Default.Equals(DisplayName, other.DisplayName)
            && EqualityComparer<string?>.Default.Equals(PnpPath, other.PnpPath)
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
            && OutputReports.SequenceEqual(other.OutputReports)
            && EqualityComparer<GameInputLightingGroups>.Default.Equals(SupportedLightingGroups, other.SupportedLightingGroups)
            && EqualityComparer<uint?>.Default.Equals(XInputDeviceIndex, other.XInputDeviceIndex)
            && FeatureReports.SequenceEqual(other.FeatureReports)
            && TouchSurfaces.SequenceEqual(other.TouchSurfaces);
    }

    /// <summary>Computes a hash of copied contents. 計算已複製內容的雜湊碼。</summary>
    /// <returns>The hash code. 雜湊碼。</returns>
    public override int GetHashCode()
    {
        int hash = 17;
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<GameInputDeviceIdentity>.Default.GetHashCode(Identity!));
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<string?>.Default.GetHashCode(DisplayName!));
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<string?>.Default.GetHashCode(PnpPath!));
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<GameInputKeyboardInfo?>.Default.GetHashCode(Keyboard!));
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<GameInputMouseInfo?>.Default.GetHashCode(Mouse!));
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<GameInputSensorsInfo?>.Default.GetHashCode(Sensors!));
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<GameInputControllerInfoSnapshot?>.Default.GetHashCode(Controller!));
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<GameInputArcadeStickInfo?>.Default.GetHashCode(ArcadeStick!));
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<GameInputFlightStickInfo?>.Default.GetHashCode(FlightStick!));
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<GameInputGamepadInfo?>.Default.GetHashCode(Gamepad!));
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<GameInputRacingWheelInfo?>.Default.GetHashCode(RacingWheel!));
        hash = HashCodeCombiner.CombineRange(hash, ForceFeedbackMotors);
        hash = HashCodeCombiner.CombineRange(hash, InputReports);
        hash = HashCodeCombiner.CombineRange(hash, OutputReports);
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<GameInputLightingGroups>.Default.GetHashCode(SupportedLightingGroups!));
        hash = HashCodeCombiner.Combine(hash, EqualityComparer<uint?>.Default.GetHashCode(XInputDeviceIndex!));
        hash = HashCodeCombiner.CombineRange(hash, FeatureReports);
        hash = HashCodeCombiner.CombineRange(hash, TouchSurfaces);
        return hash;
    }
}

/// <summary>Controller labels and switches copied through API 4 getters. 透過 API 4 getter 複製的控制器標籤與開關。</summary>
public readonly record struct GameInputControllerInfoSnapshot : IEquatable<GameInputControllerInfoSnapshot>
{
    internal GameInputControllerInfoSnapshot(
        IReadOnlyList<GameInputLabel> axisLabels,
        IReadOnlyList<GameInputLabel> buttonLabels,
        IReadOnlyList<GameInputControllerSwitchInfoSnapshot> switches)
    {
        AxisLabels = axisLabels;
        ButtonLabels = buttonLabels;
        Switches = switches;
    }

    /// <summary>
    /// The controller axis label list.
    /// Controller axis label 清單。
    /// </summary>
    public IReadOnlyList<GameInputLabel> AxisLabels { get => field ?? Array.Empty<GameInputLabel>(); }

    /// <summary>
    /// The controller button label list.
    /// Controller button label 清單。
    /// </summary>
    public IReadOnlyList<GameInputLabel> ButtonLabels { get => field ?? Array.Empty<GameInputLabel>(); }

    /// <summary>
    /// The controller switch information snapshot list.
    /// Controller switch 資訊快照清單。
    /// </summary>
    public IReadOnlyList<GameInputControllerSwitchInfoSnapshot> Switches { get => field ?? Array.Empty<GameInputControllerSwitchInfoSnapshot>(); }

    /// <summary>Compares copied collection contents. 比較已複製的集合內容。</summary>
    /// <param name="other">The other snapshot to compare. 要比較的另一個快照。</param>
    /// <returns>Returns true when all members of the two snapshots are equal. 兩個快照的所有欄位皆相同時，傳回 true。</returns>
    public bool Equals(GameInputControllerInfoSnapshot other)
    {
        return AxisLabels.SequenceEqual(other.AxisLabels)
            && ButtonLabels.SequenceEqual(other.ButtonLabels)
            && Switches.SequenceEqual(other.Switches);
    }

    /// <summary>
    /// Computes the hash code from the contents of <see cref="AxisLabels"/>, <see cref="ButtonLabels"/>, and
    /// <see cref="Switches"/>.
    /// 依 <see cref="AxisLabels"/>、<see cref="ButtonLabels"/>、<see cref="Switches"/> 內容計算雜湊碼。
    /// </summary>
    /// <returns>The hash code. 雜湊碼。</returns>
    public override int GetHashCode()
    {
        int hash = 17;
        hash = HashCodeCombiner.CombineRange(hash, AxisLabels);
        hash = HashCodeCombiner.CombineRange(hash, ButtonLabels);
        hash = HashCodeCombiner.CombineRange(hash, Switches);
        return hash;
    }

}

/// <summary>
/// A controller switch sub-information snapshot that holds no fixed buffers.
/// 不持有 fixed buffer 的 controller switch 子資訊快照。
/// </summary>
/// <remarks>
/// <see cref="Labels"/> is a collection member; value equality compares its contents element by element instead of comparing the
/// references of the copied arrays.
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
    /// The label list for each switch position.
    /// Switch 每個位置對應的 label 清單。
    /// </summary>
    public IReadOnlyList<GameInputLabel> Labels { get => field ?? Array.Empty<GameInputLabel>(); }

    /// <summary>
    /// The native switch kind.
    /// Switch 的原生種類。
    /// </summary>
    public GameInputSwitchKind Kind { get; }

    /// <summary>
    /// Compares the contents of <see cref="Labels"/> element by element for value equality.
    /// 逐一比較 <see cref="Labels"/> 內容的值相等性。
    /// </summary>
    /// <param name="other">The other snapshot to compare. 要比較的另一個快照。</param>
    /// <returns>Returns true when the <see cref="Kind"/> and <see cref="Labels"/> contents of the two snapshots are equal. 兩個快照的 <see cref="Kind"/> 與 <see cref="Labels"/> 內容皆相同時，傳回 true。</returns>
    public bool Equals(GameInputControllerSwitchInfoSnapshot other)
    {
        return Kind == other.Kind && Labels.SequenceEqual(other.Labels);
    }

    /// <summary>
    /// Computes the hash code from <see cref="Kind"/> and the contents of <see cref="Labels"/>.
    /// 依 <see cref="Kind"/> 與 <see cref="Labels"/> 內容計算雜湊碼。
    /// </summary>
    /// <returns>The hash code. 雜湊碼。</returns>
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
/// A haptic information snapshot that holds no native haptic buffers.
/// 不持有原生 haptic 緩衝區的觸覺資訊快照。
/// </summary>
/// <remarks>
/// <see cref="Locations"/> is a collection member; value equality compares its contents element by element instead of comparing
/// the references of the copied arrays.
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
    /// The haptic audio endpoint identifier.
    /// Haptic 音訊端點識別碼。
    /// </summary>
    public string AudioEndpointId { get => field ?? string.Empty; }

    /// <summary>
    /// The haptic location identifier list.
    /// Haptic 位置識別碼清單。
    /// </summary>
    public IReadOnlyList<Guid> Locations { get => field ?? Array.Empty<Guid>(); }

    /// <summary>
    /// Compares the contents of <see cref="Locations"/> element by element for value equality.
    /// 逐一比較 <see cref="Locations"/> 內容的值相等性。
    /// </summary>
    /// <param name="other">The other snapshot to compare. 要比較的另一個快照。</param>
    /// <returns>Returns true when the <see cref="AudioEndpointId"/> and <see cref="Locations"/> contents of the two snapshots are equal. 兩個快照的 <see cref="AudioEndpointId"/> 與 <see cref="Locations"/> 內容皆相同時，傳回 true。</returns>
    public bool Equals(GameInputHapticInfoSnapshot other)
    {
        return AudioEndpointId == other.AudioEndpointId && Locations.SequenceEqual(other.Locations);
    }

    /// <summary>
    /// Computes the hash code from <see cref="AudioEndpointId"/> and the contents of <see cref="Locations"/>.
    /// 依 <see cref="AudioEndpointId"/> 與 <see cref="Locations"/> 內容計算雜湊碼。
    /// </summary>
    /// <returns>The hash code. 雜湊碼。</returns>
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
