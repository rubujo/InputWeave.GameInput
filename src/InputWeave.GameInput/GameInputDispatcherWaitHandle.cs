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

    protected override bool ReleaseHandle()
    {
        return Win32NativeMethods.CloseHandle(handle);
    }
}
