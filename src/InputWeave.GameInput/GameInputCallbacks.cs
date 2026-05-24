using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// GameInput reading 回呼處理常式。
/// </summary>
/// <param name="reading">參數 reading。</param>
public delegate void GameInputReadingHandler(GameInputReading reading);

/// <summary>
/// GameInput 裝置回呼處理常式。
/// </summary>
/// <param name="device">選用的 GameInput 裝置篩選。</param>
/// <param name="timestamp">GameInput 時間戳記。</param>
/// <param name="currentStatus">目前裝置狀態。</param>
/// <param name="previousStatus">先前裝置狀態。</param>
public delegate void GameInputDeviceHandler(GameInputDevice device, ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus);

/// <summary>
/// GameInput system button 回呼處理常式。
/// </summary>
/// <param name="device">選用的 GameInput 裝置篩選。</param>
/// <param name="timestamp">GameInput 時間戳記。</param>
/// <param name="currentButtons">參數 currentButtons。</param>
/// <param name="previousButtons">參數 previousButtons。</param>
public delegate void GameInputSystemButtonHandler(GameInputDevice device, ulong timestamp, GameInputSystemButtons currentButtons, GameInputSystemButtons previousButtons);

/// <summary>
/// GameInput 鍵盤配置回呼處理常式。
/// </summary>
/// <param name="device">選用的 GameInput 裝置篩選。</param>
/// <param name="timestamp">GameInput 時間戳記。</param>
/// <param name="currentLayout">參數 currentLayout。</param>
/// <param name="previousLayout">參數 previousLayout。</param>
public delegate void GameInputKeyboardLayoutHandler(GameInputDevice device, ulong timestamp, uint currentLayout, uint previousLayout);
