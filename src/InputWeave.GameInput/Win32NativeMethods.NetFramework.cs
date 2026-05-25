#if NETFRAMEWORK
using System.Runtime.InteropServices;

namespace InputWeave.GameInput;

internal static class Win32NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr hObject);
}
#endif
