using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// The GameInput reading callback handler.
/// GameInput reading 回呼處理常式。
/// </summary>
/// <param name="reading">The native reading provided by the callback. 參數 reading。</param>
public delegate void GameInputReadingHandler(GameInputReading reading);

/// <summary>The unified API 4 device event handler. API 4 統一裝置事件處理常式。</summary>
/// <param name="deviceEvent">The lifecycle or capability change event. 生命週期或能力異動事件。</param>
/// <param name="device">The borrowed device, valid during this callback. 僅在此回呼執行期間有效的借用裝置。</param>
public delegate void GameInputDeviceEventHandler(GameInputDeviceEvent deviceEvent, GameInputDevice device);
