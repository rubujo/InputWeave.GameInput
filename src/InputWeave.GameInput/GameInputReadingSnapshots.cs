using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// 不持有原生讀取資料生命週期的讀取快照基底類別。
/// </summary>
public abstract class ReadingSnapshot
{
    private protected ReadingSnapshot(ulong timestamp)
    {
        Timestamp = timestamp;
    }

    /// <summary>
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; }
}

/// <summary>
/// 不持有原生讀取資料生命週期的鍵盤快照。
/// </summary>
public sealed class KeyboardReadingSnapshot : ReadingSnapshot
{
    internal KeyboardReadingSnapshot(ulong timestamp, GameInputKeyState[] keys)
        : base(timestamp)
    {
        Keys = Array.AsReadOnly((GameInputKeyState[])keys.Clone());
    }

    /// <summary>
    /// 目前按下或作用中的鍵盤按鍵狀態。
    /// </summary>
    public IReadOnlyList<GameInputKeyState> Keys { get; }
}

/// <summary>
/// 不持有原生讀取資料生命週期的滑鼠快照。
/// </summary>
public sealed class MouseReadingSnapshot : ReadingSnapshot
{
    internal MouseReadingSnapshot(ulong timestamp, GameInputMouseState state)
        : base(timestamp)
    {
        State = state;
    }

    /// <summary>
    /// 滑鼠狀態。
    /// </summary>
    public GameInputMouseState State { get; }
}

/// <summary>
/// 不持有原生讀取資料生命週期的感測器快照。
/// </summary>
public sealed class SensorsReadingSnapshot : ReadingSnapshot
{
    internal SensorsReadingSnapshot(ulong timestamp, GameInputSensorsState state)
        : base(timestamp)
    {
        State = state;
    }

    /// <summary>
    /// 感測器狀態。
    /// </summary>
    public GameInputSensorsState State { get; }
}

/// <summary>
/// 不持有原生讀取資料生命週期的一般 controller 快照。
/// </summary>
public sealed class ControllerReadingSnapshot : ReadingSnapshot
{
    internal ControllerReadingSnapshot(ulong timestamp, float[] axes, bool[] buttons, GameInputSwitchPosition[] switches)
        : base(timestamp)
    {
        Axes = Array.AsReadOnly((float[])axes.Clone());
        Buttons = Array.AsReadOnly((bool[])buttons.Clone());
        Switches = Array.AsReadOnly((GameInputSwitchPosition[])switches.Clone());
    }

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
}

/// <summary>
/// 不持有原生讀取資料生命週期的 arcade stick 快照。
/// </summary>
public sealed class ArcadeStickReadingSnapshot : ReadingSnapshot
{
    internal ArcadeStickReadingSnapshot(ulong timestamp, GameInputArcadeStickState state)
        : base(timestamp)
    {
        State = state;
    }

    /// <summary>
    /// Arcade stick 狀態。
    /// </summary>
    public GameInputArcadeStickState State { get; }
}

/// <summary>
/// 不持有原生讀取資料生命週期的 flight stick 快照。
/// </summary>
public sealed class FlightStickReadingSnapshot : ReadingSnapshot
{
    internal FlightStickReadingSnapshot(ulong timestamp, GameInputFlightStickState state)
        : base(timestamp)
    {
        State = state;
    }

    /// <summary>
    /// Flight stick 狀態。
    /// </summary>
    public GameInputFlightStickState State { get; }
}

/// <summary>
/// 不持有原生讀取資料生命週期的 racing wheel 快照。
/// </summary>
public sealed class RacingWheelReadingSnapshot : ReadingSnapshot
{
    internal RacingWheelReadingSnapshot(ulong timestamp, GameInputRacingWheelState state)
        : base(timestamp)
    {
        State = state;
    }

    /// <summary>
    /// Racing wheel 狀態。
    /// </summary>
    public GameInputRacingWheelState State { get; }
}

/// <summary>
/// 不持有原生 raw report 生命週期的 raw device report 快照。
/// </summary>
public sealed class RawDeviceReportSnapshot : ReadingSnapshot
{
    internal RawDeviceReportSnapshot(ulong timestamp, GameInputRawDeviceReportInfo info, byte[] data)
        : base(timestamp)
    {
        Info = info;
        Data = Array.AsReadOnly((byte[])data.Clone());
    }

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
}
