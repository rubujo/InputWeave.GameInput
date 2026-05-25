#if NETFRAMEWORK
using System.Runtime.InteropServices;

namespace InputWeave.GameInput;

internal static partial class Win32NativeMethods
{
    internal static readonly IntPtr s_hkeyLocalMachine = new(unchecked((int)0x80000002));

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr LoadLibraryEx(string lpLibFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr GetProcAddress(IntPtr hModule, IntPtr lpProcName);

    [DllImport("kernel32.dll", EntryPoint = "GetModuleFileNameW", SetLastError = true, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint GetModuleFileName(IntPtr hModule, IntPtr lpFilename, uint nSize);

    [DllImport("kernel32.dll", EntryPoint = "GetSystemDirectoryW", SetLastError = true, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint GetSystemDirectory(IntPtr lpBuffer, uint uSize);

    [DllImport("advapi32.dll", EntryPoint = "RegGetValueW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern int RegGetValue(IntPtr hkey, string lpSubKey, string lpValue, uint dwFlags, IntPtr pdwType, IntPtr pvData, ref uint pcbData);

    [DllImport("version.dll", EntryPoint = "GetFileVersionInfoSizeW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint GetFileVersionInfoSize(string lptstrFilename, out uint lpdwHandle);

    [DllImport("version.dll", EntryPoint = "GetFileVersionInfoW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetFileVersionInfo(string lptstrFilename, uint dwHandle, uint dwLen, IntPtr lpData);

    [DllImport("version.dll", EntryPoint = "VerQueryValueW", SetLastError = true, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool VerQueryValue(IntPtr pBlock, IntPtr lpSubBlock, out IntPtr lplpBuffer, out uint puLen);
}
#endif
