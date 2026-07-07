using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// A keyboard snapshot that holds no native reading lifetime.
/// 不持有原生讀取資料生命週期的鍵盤快照。
/// </summary>
/// <remarks>
/// <see cref="Keys"/> is a collection member; value equality compares the array contents element by element instead of comparing
/// the references of the copied arrays.
/// <see cref="Keys"/> 是集合欄位，值相等性採逐一比較陣列內容，而不是比較複本陣列的參考。
/// </remarks>
public readonly record struct KeyboardReadingSnapshot : IEquatable<KeyboardReadingSnapshot>
{
    internal KeyboardReadingSnapshot(ulong timestamp, GameInputKeyState[] keys)
    {
        Timestamp = timestamp;
        Keys = Array.AsReadOnly((GameInputKeyState[])keys.Clone());
    }

    /// <summary>
    /// The GameInput timestamp.
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// The currently pressed or active keyboard key states.
    /// 目前按下或作用中的鍵盤按鍵狀態。
    /// </summary>
    public IReadOnlyList<GameInputKeyState> Keys { get; }

    /// <summary>
    /// Compares the contents of <see cref="Keys"/> element by element for value equality.
    /// 逐一比較 <see cref="Keys"/> 內容的值相等性。
    /// </summary>
    /// <param name="other">The other snapshot to compare. 要比較的另一個快照。</param>
    /// <returns>Returns true when the timestamps and key contents of the two snapshots are equal. 兩個快照的時間戳記與按鍵內容皆相同時，傳回 true。</returns>
    public bool Equals(KeyboardReadingSnapshot other)
    {
        return Timestamp == other.Timestamp && Keys.SequenceEqual(other.Keys);
    }

    /// <summary>
    /// Computes the hash code from <see cref="Timestamp"/> and the contents of <see cref="Keys"/>.
    /// 依 <see cref="Timestamp"/> 與 <see cref="Keys"/> 內容計算雜湊碼。
    /// </summary>
    /// <returns>The hash code. 雜湊碼。</returns>
    public override int GetHashCode()
    {
        return HashCodeCombiner.CombineRange(Timestamp.GetHashCode(), Keys);
    }
}

/// <summary>
/// A mouse snapshot that holds no native reading lifetime.
/// 不持有原生讀取資料生命週期的滑鼠快照。
/// </summary>
public readonly record struct MouseReadingSnapshot
{
    internal MouseReadingSnapshot(ulong timestamp, GameInputMouseState state)
    {
        Timestamp = timestamp;
        State = state;
    }

    /// <summary>
    /// The GameInput timestamp.
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// The mouse state.
    /// 滑鼠狀態。
    /// </summary>
    public GameInputMouseState State { get; }
}

/// <summary>
/// A sensors snapshot that holds no native reading lifetime.
/// 不持有原生讀取資料生命週期的感測器快照。
/// </summary>
public readonly record struct SensorsReadingSnapshot
{
    internal SensorsReadingSnapshot(ulong timestamp, GameInputSensorsState state)
    {
        Timestamp = timestamp;
        State = state;
    }

    /// <summary>
    /// The GameInput timestamp.
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// The sensors state.
    /// 感測器狀態。
    /// </summary>
    public GameInputSensorsState State { get; }
}

/// <summary>
/// A controller snapshot that holds no native reading lifetime.
/// 不持有原生讀取資料生命週期的一般 controller 快照。
/// </summary>
/// <remarks>
/// <see cref="Axes"/>, <see cref="Buttons"/>, and <see cref="Switches"/> are collection members; value equality compares the
/// array contents element by element instead of comparing the references of the copied arrays.
/// <see cref="Axes"/>、<see cref="Buttons"/>、<see cref="Switches"/> 是集合欄位，
/// 值相等性採逐一比較陣列內容，而不是比較複本陣列的參考。
/// </remarks>
public readonly record struct ControllerReadingSnapshot : IEquatable<ControllerReadingSnapshot>
{
    internal ControllerReadingSnapshot(ulong timestamp, float[] axes, bool[] buttons, GameInputSwitchPosition[] switches)
    {
        Timestamp = timestamp;
        Axes = Array.AsReadOnly((float[])axes.Clone());
        Buttons = Array.AsReadOnly((bool[])buttons.Clone());
        Switches = Array.AsReadOnly((GameInputSwitchPosition[])switches.Clone());
    }

    /// <summary>
    /// The GameInput timestamp.
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// The controller axis states.
    /// Controller 軸狀態。
    /// </summary>
    public IReadOnlyList<float> Axes { get; }

    /// <summary>
    /// The controller button states.
    /// Controller 按鈕狀態。
    /// </summary>
    public IReadOnlyList<bool> Buttons { get; }

    /// <summary>
    /// The controller switch states.
    /// Controller switch 狀態。
    /// </summary>
    public IReadOnlyList<GameInputSwitchPosition> Switches { get; }

    /// <summary>
    /// Compares the contents of <see cref="Axes"/>, <see cref="Buttons"/>, and <see cref="Switches"/> element by element for
    /// value equality.
    /// 逐一比較 <see cref="Axes"/>、<see cref="Buttons"/>、<see cref="Switches"/> 內容的值相等性。
    /// </summary>
    /// <param name="other">The other snapshot to compare. 要比較的另一個快照。</param>
    /// <returns>Returns true when the timestamps and the axis, button, and switch contents of the two snapshots are equal. 兩個快照的時間戳記與軸／按鈕／switch 內容皆相同時，傳回 true。</returns>
    public bool Equals(ControllerReadingSnapshot other)
    {
        return Timestamp == other.Timestamp
            && Axes.SequenceEqual(other.Axes)
            && Buttons.SequenceEqual(other.Buttons)
            && Switches.SequenceEqual(other.Switches);
    }

    /// <summary>
    /// Computes the hash code from <see cref="Timestamp"/> and the contents of each collection member.
    /// 依 <see cref="Timestamp"/> 與各集合欄位內容計算雜湊碼。
    /// </summary>
    /// <returns>The hash code. 雜湊碼。</returns>
    public override int GetHashCode()
    {
        int hash = Timestamp.GetHashCode();
        hash = HashCodeCombiner.CombineRange(hash, Axes);
        hash = HashCodeCombiner.CombineRange(hash, Buttons);
        hash = HashCodeCombiner.CombineRange(hash, Switches);
        return hash;
    }
}

/// <summary>
/// An arcade stick snapshot that holds no native reading lifetime.
/// 不持有原生讀取資料生命週期的 arcade stick 快照。
/// </summary>
public readonly record struct ArcadeStickReadingSnapshot
{
    internal ArcadeStickReadingSnapshot(ulong timestamp, GameInputArcadeStickState state)
    {
        Timestamp = timestamp;
        State = state;
    }

    /// <summary>
    /// The GameInput timestamp.
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// The arcade stick state.
    /// Arcade stick 狀態。
    /// </summary>
    public GameInputArcadeStickState State { get; }
}

/// <summary>
/// A flight stick snapshot that holds no native reading lifetime.
/// 不持有原生讀取資料生命週期的 flight stick 快照。
/// </summary>
public readonly record struct FlightStickReadingSnapshot
{
    internal FlightStickReadingSnapshot(ulong timestamp, GameInputFlightStickState state)
    {
        Timestamp = timestamp;
        State = state;
    }

    /// <summary>
    /// The GameInput timestamp.
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// The flight stick state.
    /// Flight stick 狀態。
    /// </summary>
    public GameInputFlightStickState State { get; }
}

/// <summary>
/// A racing wheel snapshot that holds no native reading lifetime.
/// 不持有原生讀取資料生命週期的 racing wheel 快照。
/// </summary>
public readonly record struct RacingWheelReadingSnapshot
{
    internal RacingWheelReadingSnapshot(ulong timestamp, GameInputRacingWheelState state)
    {
        Timestamp = timestamp;
        State = state;
    }

    /// <summary>
    /// The GameInput timestamp.
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// The racing wheel state.
    /// Racing wheel 狀態。
    /// </summary>
    public GameInputRacingWheelState State { get; }
}

/// <summary>
/// A raw device report snapshot that holds no native raw report lifetime.
/// 不持有原生 raw report 生命週期的 raw device report 快照。
/// </summary>
/// <remarks>
/// <see cref="Data"/> is a collection member; value equality compares the array contents element by element instead of comparing
/// the references of the copied arrays.
/// <see cref="Data"/> 是集合欄位，值相等性採逐一比較陣列內容，而不是比較複本陣列的參考。
/// </remarks>
public readonly record struct RawDeviceReportSnapshot : IEquatable<RawDeviceReportSnapshot>
{
    internal RawDeviceReportSnapshot(ulong timestamp, GameInputRawDeviceReportInfo info, byte[] data)
    {
        Timestamp = timestamp;
        Info = info;
        Data = Array.AsReadOnly((byte[])data.Clone());
    }

    /// <summary>
    /// The GameInput timestamp.
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// The raw device report information.
    /// Raw device report 資訊。
    /// </summary>
    public GameInputRawDeviceReportInfo Info { get; }

    /// <summary>
    /// The copied raw report data.
    /// 已複製的 raw report 資料。
    /// </summary>
    public IReadOnlyList<byte> Data { get; }

    /// <summary>
    /// Gets a new array of the raw report data.
    /// 取得 raw report 資料的新陣列。
    /// </summary>
    /// <returns>A copy of the raw report data. Raw report 資料複本。</returns>
    public byte[] GetData()
    {
        return [.. Data];
    }

    /// <summary>
    /// Compares the contents of <see cref="Data"/> element by element for value equality.
    /// 逐一比較 <see cref="Data"/> 內容的值相等性。
    /// </summary>
    /// <param name="other">The other snapshot to compare. 要比較的另一個快照。</param>
    /// <returns>Returns true when the timestamps, report information, and data contents of the two snapshots are equal. 兩個快照的時間戳記、報告資訊與資料內容皆相同時，傳回 true。</returns>
    public bool Equals(RawDeviceReportSnapshot other)
    {
        return Timestamp == other.Timestamp && Info.Equals(other.Info) && Data.SequenceEqual(other.Data);
    }

    /// <summary>
    /// Computes the hash code from <see cref="Timestamp"/>, <see cref="Info"/>, and the contents of <see cref="Data"/>.
    /// 依 <see cref="Timestamp"/>、<see cref="Info"/> 與 <see cref="Data"/> 內容計算雜湊碼。
    /// </summary>
    /// <returns>The hash code. 雜湊碼。</returns>
    public override int GetHashCode()
    {
        int hash = HashCodeCombiner.Combine(Timestamp.GetHashCode(), Info.GetHashCode());
        return HashCodeCombiner.CombineRange(hash, Data);
    }
}
