using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// A gamepad snapshot that holds no native reading lifetime.
/// 不持有原生讀取資料生命週期的 gamepad 快照。
/// </summary>
public readonly record struct GamepadReadingSnapshot
{
    internal GamepadReadingSnapshot(ulong timestamp, GameInputGamepadState state)
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
    /// The gamepad state.
    /// Gamepad 狀態。
    /// </summary>
    public GameInputGamepadState State { get; }
}
