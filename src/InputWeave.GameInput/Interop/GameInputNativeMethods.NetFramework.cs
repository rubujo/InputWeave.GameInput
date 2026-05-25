#if NETFRAMEWORK
using System.Runtime.InteropServices;

namespace InputWeave.GameInput.Interop;

internal static partial class GameInputNativeMethods
{
    [DllImport(GameInputConstants.DllName, EntryPoint = "GameInputInitialize", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern int GameInputInitialize(ref Guid riid, out IntPtr ppv);
}
#endif
