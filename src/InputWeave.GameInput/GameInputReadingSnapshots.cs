using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// 不持有原生讀取資料生命週期的鍵盤快照。
/// </summary>
/// <remarks>
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
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// 目前按下或作用中的鍵盤按鍵狀態。
    /// </summary>
    public IReadOnlyList<GameInputKeyState> Keys { get; }

    /// <summary>
    /// 逐一比較 <see cref="Keys"/> 內容的值相等性。
    /// </summary>
    /// <param name="other">要比較的另一個快照。</param>
    /// <returns>兩個快照的時間戳記與按鍵內容皆相同時，傳回 true。</returns>
    public bool Equals(KeyboardReadingSnapshot other)
    {
        return Timestamp == other.Timestamp && Keys.SequenceEqual(other.Keys);
    }

    /// <summary>
    /// 依 <see cref="Timestamp"/> 與 <see cref="Keys"/> 內容計算雜湊碼。
    /// </summary>
    /// <returns>雜湊碼。</returns>
    public override int GetHashCode()
    {
        return HashCodeCombiner.CombineRange(Timestamp.GetHashCode(), Keys);
    }
}

/// <summary>
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
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// 滑鼠狀態。
    /// </summary>
    public GameInputMouseState State { get; }
}

/// <summary>
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
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// 感測器狀態。
    /// </summary>
    public GameInputSensorsState State { get; }
}

/// <summary>
/// 不持有原生讀取資料生命週期的一般 controller 快照。
/// </summary>
/// <remarks>
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
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// Controller 軸狀態。
    /// </summary>
    public IReadOnlyList<float> Axes { get; }

    /// <summary>
    /// Controller 按鈕狀態。
    /// </summary>
    public IReadOnlyList<bool> Buttons { get; }

    /// <summary>
    /// Controller switch 狀態。
    /// </summary>
    public IReadOnlyList<GameInputSwitchPosition> Switches { get; }

    /// <summary>
    /// 逐一比較 <see cref="Axes"/>、<see cref="Buttons"/>、<see cref="Switches"/> 內容的值相等性。
    /// </summary>
    /// <param name="other">要比較的另一個快照。</param>
    /// <returns>兩個快照的時間戳記與軸／按鈕／switch 內容皆相同時，傳回 true。</returns>
    public bool Equals(ControllerReadingSnapshot other)
    {
        return Timestamp == other.Timestamp
            && Axes.SequenceEqual(other.Axes)
            && Buttons.SequenceEqual(other.Buttons)
            && Switches.SequenceEqual(other.Switches);
    }

    /// <summary>
    /// 依 <see cref="Timestamp"/> 與各集合欄位內容計算雜湊碼。
    /// </summary>
    /// <returns>雜湊碼。</returns>
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
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// Arcade stick 狀態。
    /// </summary>
    public GameInputArcadeStickState State { get; }
}

/// <summary>
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
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// Flight stick 狀態。
    /// </summary>
    public GameInputFlightStickState State { get; }
}

/// <summary>
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
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// Racing wheel 狀態。
    /// </summary>
    public GameInputRacingWheelState State { get; }
}

/// <summary>
/// 不持有原生 raw report 生命週期的 raw device report 快照。
/// </summary>
/// <remarks>
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
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// Raw device report 資訊。
    /// </summary>
    public GameInputRawDeviceReportInfo Info { get; }

    /// <summary>
    /// 已複製的 raw report 資料。
    /// </summary>
    public IReadOnlyList<byte> Data { get; }

    /// <summary>
    /// 取得 raw report 資料的新陣列。
    /// </summary>
    /// <returns>Raw report 資料複本。</returns>
    public byte[] GetData()
    {
        return [.. Data];
    }

    /// <summary>
    /// 逐一比較 <see cref="Data"/> 內容的值相等性。
    /// </summary>
    /// <param name="other">要比較的另一個快照。</param>
    /// <returns>兩個快照的時間戳記、報告資訊與資料內容皆相同時，傳回 true。</returns>
    public bool Equals(RawDeviceReportSnapshot other)
    {
        return Timestamp == other.Timestamp && Info.Equals(other.Info) && Data.SequenceEqual(other.Data);
    }

    /// <summary>
    /// 依 <see cref="Timestamp"/>、<see cref="Info"/> 與 <see cref="Data"/> 內容計算雜湊碼。
    /// </summary>
    /// <returns>雜湊碼。</returns>
    public override int GetHashCode()
    {
        int hash = HashCodeCombiner.Combine(Timestamp.GetHashCode(), Info.GetHashCode());
        return HashCodeCombiner.CombineRange(hash, Data);
    }
}
