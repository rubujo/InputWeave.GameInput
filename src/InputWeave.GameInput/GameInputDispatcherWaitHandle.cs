using Microsoft.Win32.SafeHandles;

namespace InputWeave.GameInput;

/// <summary>
/// The safe wrapper for the GameInput dispatcher wait handle.
/// GameInput dispatcher wait handle 的安全包裝。
/// </summary>
public sealed class GameInputDispatcherWaitHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal GameInputDispatcherWaitHandle(IntPtr handle)
        : base(ownsHandle: true)
    {
        SetHandle(handle);
    }

    /// <summary>
    /// Releases the native wait handle returned by the GameInput dispatcher.
    /// 釋放 GameInput dispatcher 傳回的原生 wait handle。
    /// </summary>
    /// <returns>Returns true when the handle was released successfully. 成功釋放 handle 時傳回 true。</returns>
    protected override bool ReleaseHandle()
    {
        return Win32NativeMethods.CloseHandle(handle);
    }
}
