using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// The GameInput reading callback handler.
/// GameInput reading 回呼處理常式。
/// </summary>
/// <param name="reading">The native reading provided by the callback. 參數 reading。</param>
public delegate void GameInputReadingHandler(GameInputReading reading);

/// <summary>
/// The GameInput device callback handler.
/// GameInput 裝置回呼處理常式。
/// </summary>
/// <param name="device">The GameInput device that raised the callback. 觸發回呼的 GameInput 裝置。</param>
/// <param name="timestamp">The GameInput timestamp. GameInput 時間戳記。</param>
/// <param name="currentStatus">The current device status. 目前裝置狀態。</param>
/// <param name="previousStatus">The previous device status. 先前裝置狀態。</param>
public delegate void GameInputDeviceHandler(GameInputDevice device, ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus);

/// <summary>
/// The GameInput system button callback handler.
/// GameInput system button 回呼處理常式。
/// </summary>
/// <param name="device">The GameInput device that raised the callback. 觸發回呼的 GameInput 裝置。</param>
/// <param name="timestamp">The GameInput timestamp. GameInput 時間戳記。</param>
/// <param name="currentButtons">The current system button state. 目前 system button 狀態。</param>
/// <param name="previousButtons">The previous system button state. 先前 system button 狀態。</param>
public delegate void GameInputSystemButtonHandler(GameInputDevice device, ulong timestamp, GameInputSystemButtons currentButtons, GameInputSystemButtons previousButtons);

/// <summary>
/// The GameInput keyboard layout callback handler.
/// GameInput 鍵盤配置回呼處理常式。
/// </summary>
/// <param name="device">The GameInput device that raised the callback. 觸發回呼的 GameInput 裝置。</param>
/// <param name="timestamp">The GameInput timestamp. GameInput 時間戳記。</param>
/// <param name="currentLayout">The current keyboard layout identifier. 目前鍵盤配置識別值。</param>
/// <param name="previousLayout">The previous keyboard layout identifier. 先前鍵盤配置識別值。</param>
public delegate void GameInputKeyboardLayoutHandler(GameInputDevice device, ulong timestamp, uint currentLayout, uint previousLayout);
