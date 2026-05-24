using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput
{
    /// <summary>
    /// GameInput reading 回呼處理常式。
    /// </summary>
    /// <param name="reading">回呼期間有效的 reading 包裝。</param>
    public delegate void GameInputReadingHandler(GameInputReading reading);

    /// <summary>
    /// GameInput 裝置回呼處理常式。
    /// </summary>
    /// <param name="device">回呼期間有效的裝置包裝。</param>
    /// <param name="timestamp">GameInput 時間戳記。</param>
    /// <param name="currentStatus">目前裝置狀態。</param>
    /// <param name="previousStatus">先前裝置狀態。</param>
    public delegate void GameInputDeviceHandler(GameInputDevice device, ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus);

    /// <summary>
    /// GameInput system button 回呼處理常式。
    /// </summary>
    /// <param name="device">回呼期間有效的裝置包裝。</param>
    /// <param name="timestamp">GameInput 時間戳記。</param>
    /// <param name="currentButtons">目前 system button 狀態。</param>
    /// <param name="previousButtons">先前 system button 狀態。</param>
    public delegate void GameInputSystemButtonHandler(GameInputDevice device, ulong timestamp, GameInputSystemButtons currentButtons, GameInputSystemButtons previousButtons);

    /// <summary>
    /// GameInput 鍵盤配置回呼處理常式。
    /// </summary>
    /// <param name="device">回呼期間有效的裝置包裝。</param>
    /// <param name="timestamp">GameInput 時間戳記。</param>
    /// <param name="currentLayout">目前鍵盤配置識別碼。</param>
    /// <param name="previousLayout">先前鍵盤配置識別碼。</param>
    public delegate void GameInputKeyboardLayoutHandler(GameInputDevice device, ulong timestamp, uint currentLayout, uint previousLayout);
}
