using Microsoft.Win32.SafeHandles;

namespace InputWeave.GameInput;

/// <summary>
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
    /// 釋放 GameInput dispatcher 傳回的原生 wait handle。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    protected override bool ReleaseHandle()
    {
        return Win32NativeMethods.CloseHandle(handle);
    }
}
