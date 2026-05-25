#if NET10_0_OR_GREATER
using System.Runtime.InteropServices;

namespace InputWeave.GameInput;

internal static partial class Win32NativeMethods
{
    internal static readonly IntPtr s_hkeyLocalMachine = new(unchecked((int)0x80000002));

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(IntPtr hObject);

    [LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial IntPtr LoadLibraryEx(string lpLibFileName, IntPtr hFile, uint dwFlags);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FreeLibrary(IntPtr hModule);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial IntPtr GetProcAddress(IntPtr hModule, IntPtr lpProcName);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleFileNameW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint GetModuleFileName(IntPtr hModule, IntPtr lpFilename, uint nSize);

    [LibraryImport("kernel32.dll", EntryPoint = "GetSystemDirectoryW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint GetSystemDirectory(IntPtr lpBuffer, uint uSize);

    [LibraryImport("advapi32.dll", EntryPoint = "RegGetValueW", StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int RegGetValue(IntPtr hkey, string lpSubKey, string lpValue, uint dwFlags, IntPtr pdwType, IntPtr pvData, ref uint pcbData);

    [LibraryImport("version.dll", EntryPoint = "GetFileVersionInfoSizeW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint GetFileVersionInfoSize(string lptstrFilename, out uint lpdwHandle);

    [LibraryImport("version.dll", EntryPoint = "GetFileVersionInfoW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetFileVersionInfo(string lptstrFilename, uint dwHandle, uint dwLen, IntPtr lpData);

    [LibraryImport("version.dll", EntryPoint = "VerQueryValueW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VerQueryValue(IntPtr pBlock, IntPtr lpSubBlock, out IntPtr lplpBuffer, out uint puLen);
}
#endif
