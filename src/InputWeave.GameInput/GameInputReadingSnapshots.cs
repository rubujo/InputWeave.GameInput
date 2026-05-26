using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// 不持有原生讀取資料生命週期的鍵盤快照。
/// </summary>
public sealed class KeyboardReadingSnapshot
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
}

/// <summary>
/// 不持有原生讀取資料生命週期的滑鼠快照。
/// </summary>
public sealed class MouseReadingSnapshot
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
public sealed class SensorsReadingSnapshot
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
public sealed class ControllerReadingSnapshot
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
}

/// <summary>
/// 不持有原生讀取資料生命週期的 arcade stick 快照。
/// </summary>
public sealed class ArcadeStickReadingSnapshot
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
public sealed class FlightStickReadingSnapshot
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
public sealed class RacingWheelReadingSnapshot
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
public sealed class RawDeviceReportSnapshot
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
}
