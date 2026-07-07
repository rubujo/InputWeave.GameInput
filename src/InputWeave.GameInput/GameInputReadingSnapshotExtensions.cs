using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// Provides button edge-detection helpers for reading snapshots: checking whether a button is currently down, and comparing
/// against the previous-frame snapshot for just-pressed and just-released states, so polling game loops do not need to implement
/// flag comparison themselves.
/// 提供 reading 快照的按鈕邊緣偵測輔助方法：判斷按鈕目前是否按下，以及與上一影格快照比較的
/// 「剛按下」（pressed）／「剛放開」（released）狀態，讓輪詢式遊戲迴圈不需要自行實作旗標比較。
/// </summary>
public static class GameInputReadingSnapshotExtensions
{
    /// <summary>
    /// Determines whether all specified buttons in the gamepad snapshot are down.
    /// 判斷 gamepad 快照中指定按鈕是否全部處於按下狀態。
    /// </summary>
    /// <param name="snapshot">The snapshot to check. 要檢查的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when all specified buttons are down; passing <see cref="GameInputGamepadButtons.GameInputGamepadNone"/> always returns false. 指定按鈕全部按下時傳回 true；傳入 <see cref="GameInputGamepadButtons.GameInputGamepadNone"/> 一律傳回 false。</returns>
    public static bool IsButtonDown(this in GamepadReadingSnapshot snapshot, GameInputGamepadButtons buttons)
    {
        return buttons != GameInputGamepadButtons.GameInputGamepadNone
            && (snapshot.State.Buttons & buttons) == buttons;
    }

    /// <summary>
    /// Determines whether a gamepad button was just pressed this frame (down in the current snapshot and not down in the
    /// previous-frame snapshot).
    /// 判斷 gamepad 按鈕是否在這一影格剛被按下（目前快照按下、上一影格快照未按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when the button is down in the current snapshot and was not down in the previous-frame snapshot. 按鈕在目前快照按下且在上一影格快照未按下時，傳回 true。</returns>
    public static bool WasButtonPressed(this in GamepadReadingSnapshot current, in GamepadReadingSnapshot previous, GameInputGamepadButtons buttons)
    {
        return current.IsButtonDown(buttons) && !previous.IsButtonDown(buttons);
    }

    /// <summary>
    /// Determines whether a gamepad button was just released this frame (not down in the current snapshot and down in the
    /// previous-frame snapshot).
    /// 判斷 gamepad 按鈕是否在這一影格剛被放開（目前快照未按下、上一影格快照按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when the button is not down in the current snapshot and was down in the previous-frame snapshot. 按鈕在目前快照未按下且在上一影格快照按下時，傳回 true。</returns>
    public static bool WasButtonReleased(this in GamepadReadingSnapshot current, in GamepadReadingSnapshot previous, GameInputGamepadButtons buttons)
    {
        return !current.IsButtonDown(buttons) && previous.IsButtonDown(buttons);
    }

    /// <summary>
    /// Determines whether all specified buttons in the mouse snapshot are down.
    /// 判斷滑鼠快照中指定按鈕是否全部處於按下狀態。
    /// </summary>
    /// <param name="snapshot">The snapshot to check. 要檢查的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when all specified buttons are down; passing <see cref="GameInputMouseButtons.GameInputMouseNone"/> always returns false. 指定按鈕全部按下時傳回 true；傳入 <see cref="GameInputMouseButtons.GameInputMouseNone"/> 一律傳回 false。</returns>
    public static bool IsButtonDown(this in MouseReadingSnapshot snapshot, GameInputMouseButtons buttons)
    {
        return buttons != GameInputMouseButtons.GameInputMouseNone
            && (snapshot.State.Buttons & buttons) == buttons;
    }

    /// <summary>
    /// Determines whether a mouse button was just pressed this frame (down in the current snapshot and not down in the previous-
    /// frame snapshot).
    /// 判斷滑鼠按鈕是否在這一影格剛被按下（目前快照按下、上一影格快照未按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when the button is down in the current snapshot and was not down in the previous-frame snapshot. 按鈕在目前快照按下且在上一影格快照未按下時，傳回 true。</returns>
    public static bool WasButtonPressed(this in MouseReadingSnapshot current, in MouseReadingSnapshot previous, GameInputMouseButtons buttons)
    {
        return current.IsButtonDown(buttons) && !previous.IsButtonDown(buttons);
    }

    /// <summary>
    /// Determines whether a mouse button was just released this frame (not down in the current snapshot and down in the previous-
    /// frame snapshot).
    /// 判斷滑鼠按鈕是否在這一影格剛被放開（目前快照未按下、上一影格快照按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when the button is not down in the current snapshot and was down in the previous-frame snapshot. 按鈕在目前快照未按下且在上一影格快照按下時，傳回 true。</returns>
    public static bool WasButtonReleased(this in MouseReadingSnapshot current, in MouseReadingSnapshot previous, GameInputMouseButtons buttons)
    {
        return !current.IsButtonDown(buttons) && previous.IsButtonDown(buttons);
    }

    /// <summary>
    /// Determines whether all specified buttons in the arcade stick snapshot are down.
    /// 判斷 arcade stick 快照中指定按鈕是否全部處於按下狀態。
    /// </summary>
    /// <param name="snapshot">The snapshot to check. 要檢查的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when all specified buttons are down; passing <see cref="GameInputArcadeStickButtons.GameInputArcadeStickNone"/> always returns false. 指定按鈕全部按下時傳回 true；傳入 <see cref="GameInputArcadeStickButtons.GameInputArcadeStickNone"/> 一律傳回 false。</returns>
    public static bool IsButtonDown(this in ArcadeStickReadingSnapshot snapshot, GameInputArcadeStickButtons buttons)
    {
        return buttons != GameInputArcadeStickButtons.GameInputArcadeStickNone
            && (snapshot.State.Buttons & buttons) == buttons;
    }

    /// <summary>
    /// Determines whether an arcade stick button was just pressed this frame (down in the current snapshot and not down in the
    /// previous-frame snapshot).
    /// 判斷 arcade stick 按鈕是否在這一影格剛被按下（目前快照按下、上一影格快照未按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when the button is down in the current snapshot and was not down in the previous-frame snapshot. 按鈕在目前快照按下且在上一影格快照未按下時，傳回 true。</returns>
    public static bool WasButtonPressed(this in ArcadeStickReadingSnapshot current, in ArcadeStickReadingSnapshot previous, GameInputArcadeStickButtons buttons)
    {
        return current.IsButtonDown(buttons) && !previous.IsButtonDown(buttons);
    }

    /// <summary>
    /// Determines whether an arcade stick button was just released this frame (not down in the current snapshot and down in the
    /// previous-frame snapshot).
    /// 判斷 arcade stick 按鈕是否在這一影格剛被放開（目前快照未按下、上一影格快照按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when the button is not down in the current snapshot and was down in the previous-frame snapshot. 按鈕在目前快照未按下且在上一影格快照按下時，傳回 true。</returns>
    public static bool WasButtonReleased(this in ArcadeStickReadingSnapshot current, in ArcadeStickReadingSnapshot previous, GameInputArcadeStickButtons buttons)
    {
        return !current.IsButtonDown(buttons) && previous.IsButtonDown(buttons);
    }

    /// <summary>
    /// Determines whether all specified buttons in the flight stick snapshot are down.
    /// 判斷 flight stick 快照中指定按鈕是否全部處於按下狀態。
    /// </summary>
    /// <param name="snapshot">The snapshot to check. 要檢查的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when all specified buttons are down; passing <see cref="GameInputFlightStickButtons.GameInputFlightStickNone"/> always returns false. 指定按鈕全部按下時傳回 true；傳入 <see cref="GameInputFlightStickButtons.GameInputFlightStickNone"/> 一律傳回 false。</returns>
    public static bool IsButtonDown(this in FlightStickReadingSnapshot snapshot, GameInputFlightStickButtons buttons)
    {
        return buttons != GameInputFlightStickButtons.GameInputFlightStickNone
            && (snapshot.State.Buttons & buttons) == buttons;
    }

    /// <summary>
    /// Determines whether a flight stick button was just pressed this frame (down in the current snapshot and not down in the
    /// previous-frame snapshot).
    /// 判斷 flight stick 按鈕是否在這一影格剛被按下（目前快照按下、上一影格快照未按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when the button is down in the current snapshot and was not down in the previous-frame snapshot. 按鈕在目前快照按下且在上一影格快照未按下時，傳回 true。</returns>
    public static bool WasButtonPressed(this in FlightStickReadingSnapshot current, in FlightStickReadingSnapshot previous, GameInputFlightStickButtons buttons)
    {
        return current.IsButtonDown(buttons) && !previous.IsButtonDown(buttons);
    }

    /// <summary>
    /// Determines whether a flight stick button was just released this frame (not down in the current snapshot and down in the
    /// previous-frame snapshot).
    /// 判斷 flight stick 按鈕是否在這一影格剛被放開（目前快照未按下、上一影格快照按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when the button is not down in the current snapshot and was down in the previous-frame snapshot. 按鈕在目前快照未按下且在上一影格快照按下時，傳回 true。</returns>
    public static bool WasButtonReleased(this in FlightStickReadingSnapshot current, in FlightStickReadingSnapshot previous, GameInputFlightStickButtons buttons)
    {
        return !current.IsButtonDown(buttons) && previous.IsButtonDown(buttons);
    }

    /// <summary>
    /// Determines whether all specified buttons in the racing wheel snapshot are down.
    /// 判斷 racing wheel 快照中指定按鈕是否全部處於按下狀態。
    /// </summary>
    /// <param name="snapshot">The snapshot to check. 要檢查的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when all specified buttons are down; passing <see cref="GameInputRacingWheelButtons.GameInputRacingWheelNone"/> always returns false. 指定按鈕全部按下時傳回 true；傳入 <see cref="GameInputRacingWheelButtons.GameInputRacingWheelNone"/> 一律傳回 false。</returns>
    public static bool IsButtonDown(this in RacingWheelReadingSnapshot snapshot, GameInputRacingWheelButtons buttons)
    {
        return buttons != GameInputRacingWheelButtons.GameInputRacingWheelNone
            && (snapshot.State.Buttons & buttons) == buttons;
    }

    /// <summary>
    /// Determines whether a racing wheel button was just pressed this frame (down in the current snapshot and not down in the
    /// previous-frame snapshot).
    /// 判斷 racing wheel 按鈕是否在這一影格剛被按下（目前快照按下、上一影格快照未按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when the button is down in the current snapshot and was not down in the previous-frame snapshot. 按鈕在目前快照按下且在上一影格快照未按下時，傳回 true。</returns>
    public static bool WasButtonPressed(this in RacingWheelReadingSnapshot current, in RacingWheelReadingSnapshot previous, GameInputRacingWheelButtons buttons)
    {
        return current.IsButtonDown(buttons) && !previous.IsButtonDown(buttons);
    }

    /// <summary>
    /// Determines whether a racing wheel button was just released this frame (not down in the current snapshot and down in the
    /// previous-frame snapshot).
    /// 判斷 racing wheel 按鈕是否在這一影格剛被放開（目前快照未按下、上一影格快照按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttons">The button flags to check; multiple flags may be combined, in which case all of them must be down. 要檢查的按鈕旗標；可同時指定多個旗標，此時全部按下才算成立。</param>
    /// <returns>Returns true when the button is not down in the current snapshot and was down in the previous-frame snapshot. 按鈕在目前快照未按下且在上一影格快照按下時，傳回 true。</returns>
    public static bool WasButtonReleased(this in RacingWheelReadingSnapshot current, in RacingWheelReadingSnapshot previous, GameInputRacingWheelButtons buttons)
    {
        return !current.IsButtonDown(buttons) && previous.IsButtonDown(buttons);
    }

    /// <summary>
    /// Determines whether the specified virtual key in the keyboard snapshot is down.
    /// 判斷鍵盤快照中指定虛擬鍵是否處於按下狀態。
    /// </summary>
    /// <param name="snapshot">The snapshot to check. 要檢查的快照。</param>
    /// <param name="virtualKey">The virtual key code to check (for example, 0x41 is the A key). 要檢查的虛擬鍵碼（例如 0x41 代表 A 鍵）。</param>
    /// <returns>Returns true when the specified virtual key is currently down; otherwise returns false. 指定虛擬鍵目前按下時傳回 true；否則傳回 false。</returns>
    public static bool IsKeyDown(this in KeyboardReadingSnapshot snapshot, uint virtualKey)
    {
        foreach (GameInputKeyState key in snapshot.Keys)
        {
            if (key.VirtualKey == virtualKey)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a keyboard virtual key was just pressed this frame (down in the current snapshot and not down in the
    /// previous-frame snapshot).
    /// 判斷鍵盤虛擬鍵是否在這一影格剛被按下（目前快照按下、上一影格快照未按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="virtualKey">The virtual key code to check (for example, 0x41 is the A key). 要檢查的虛擬鍵碼（例如 0x41 代表 A 鍵）。</param>
    /// <returns>Returns true when the virtual key is down in the current snapshot and was not down in the previous-frame snapshot. 虛擬鍵在目前快照按下且在上一影格快照未按下時，傳回 true。</returns>
    public static bool WasKeyPressed(this in KeyboardReadingSnapshot current, in KeyboardReadingSnapshot previous, uint virtualKey)
    {
        return current.IsKeyDown(virtualKey) && !previous.IsKeyDown(virtualKey);
    }

    /// <summary>
    /// Determines whether a keyboard virtual key was just released this frame (not down in the current snapshot and down in the
    /// previous-frame snapshot).
    /// 判斷鍵盤虛擬鍵是否在這一影格剛被放開（目前快照未按下、上一影格快照按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="virtualKey">The virtual key code to check (for example, 0x41 is the A key). 要檢查的虛擬鍵碼（例如 0x41 代表 A 鍵）。</param>
    /// <returns>Returns true when the virtual key is not down in the current snapshot and was down in the previous-frame snapshot. 虛擬鍵在目前快照未按下且在上一影格快照按下時，傳回 true。</returns>
    public static bool WasKeyReleased(this in KeyboardReadingSnapshot current, in KeyboardReadingSnapshot previous, uint virtualKey)
    {
        return !current.IsKeyDown(virtualKey) && previous.IsKeyDown(virtualKey);
    }

    /// <summary>
    /// Determines whether the button at the specified index in the controller snapshot is down.
    /// 判斷一般 controller 快照中指定索引的按鈕是否處於按下狀態。
    /// </summary>
    /// <param name="snapshot">The snapshot to check. 要檢查的快照。</param>
    /// <param name="buttonIndex">The zero-based button index to check. 要檢查的按鈕索引（從 0 起算）。</param>
    /// <returns>Returns true when the button at the specified index is currently down; an index beyond the device button count is treated as not down and returns false. 指定索引的按鈕目前按下時傳回 true；索引超出裝置按鈕數量時視為未按下，傳回 false。</returns>
    public static bool IsButtonDown(this in ControllerReadingSnapshot snapshot, int buttonIndex)
    {
        return buttonIndex >= 0
            && buttonIndex < snapshot.Buttons.Count
            && snapshot.Buttons[buttonIndex];
    }

    /// <summary>
    /// Determines whether a controller button was just pressed this frame (down in the current snapshot and not down in the
    /// previous-frame snapshot).
    /// 判斷一般 controller 按鈕是否在這一影格剛被按下（目前快照按下、上一影格快照未按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttonIndex">The zero-based button index to check. 要檢查的按鈕索引（從 0 起算）。</param>
    /// <returns>Returns true when the button is down in the current snapshot and was not down in the previous-frame snapshot. 按鈕在目前快照按下且在上一影格快照未按下時，傳回 true。</returns>
    public static bool WasButtonPressed(this in ControllerReadingSnapshot current, in ControllerReadingSnapshot previous, int buttonIndex)
    {
        return current.IsButtonDown(buttonIndex) && !previous.IsButtonDown(buttonIndex);
    }

    /// <summary>
    /// Determines whether a controller button was just released this frame (not down in the current snapshot and down in the
    /// previous-frame snapshot).
    /// 判斷一般 controller 按鈕是否在這一影格剛被放開（目前快照未按下、上一影格快照按下）。
    /// </summary>
    /// <param name="current">The snapshot of the current frame. 目前影格的快照。</param>
    /// <param name="previous">The snapshot of the previous frame. 上一影格的快照。</param>
    /// <param name="buttonIndex">The zero-based button index to check. 要檢查的按鈕索引（從 0 起算）。</param>
    /// <returns>Returns true when the button is not down in the current snapshot and was down in the previous-frame snapshot. 按鈕在目前快照未按下且在上一影格快照按下時，傳回 true。</returns>
    public static bool WasButtonReleased(this in ControllerReadingSnapshot current, in ControllerReadingSnapshot previous, int buttonIndex)
    {
        return !current.IsButtonDown(buttonIndex) && previous.IsButtonDown(buttonIndex);
    }
}
