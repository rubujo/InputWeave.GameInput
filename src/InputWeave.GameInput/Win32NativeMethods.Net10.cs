#if NET10_0_OR_GREATER
using System.Runtime.InteropServices;

namespace InputWeave.GameInput;

internal static partial class Win32NativeMethods
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(IntPtr hObject);
}
#endif
