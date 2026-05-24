using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// 不持有原生讀取資料生命週期的 gamepad 快照。
/// </summary>
/// <remarks>
/// 建立 gamepad 快照。
/// </remarks>
/// <param name="timestamp">GameInput 時間戳記。</param>
/// <param name="state">Gamepad 狀態。</param>
/// <returns>操作完成後的查詢或建立結果。</returns>
public sealed class GamepadReadingSnapshot(ulong timestamp, GameInputGamepadState state)
{
    /// <summary>
    /// GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; } = timestamp;

    /// <summary>
    /// Gamepad 狀態。
    /// </summary>
    public GameInputGamepadState State { get; } = state;
}
