using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace InputWeave.GameInput
{
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

    internal static class Win32NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);
    }
}
