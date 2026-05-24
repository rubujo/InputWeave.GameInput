using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput
{
    /// <summary>
    /// 不持有原生讀取資料生命週期的 gamepad 快照。
    /// </summary>
    public sealed class GamepadReadingSnapshot
    {
        /// <summary>
        /// 建立 gamepad 快照。
        /// </summary>
        /// <param name="timestamp">GameInput 時間戳記。</param>
        /// <param name="state">Gamepad 狀態。</param>
        public GamepadReadingSnapshot(ulong timestamp, GameInputGamepadState state)
        {
            Timestamp = timestamp;
            State = state;
        }

        /// <summary>
        /// GameInput 時間戳記。
        /// </summary>
        public ulong Timestamp { get; }

        /// <summary>
        /// Gamepad 狀態。
        /// </summary>
        public GameInputGamepadState State { get; }
    }
}
