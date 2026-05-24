#if NET10_0_OR_GREATER
using System;
using System.Runtime.InteropServices;

namespace InputWeave.GameInput.Interop;

internal static partial class GameInputNativeMethods
{
    [LibraryImport(GameInputConstants.DllName, EntryPoint = "GameInputInitialize")]
    internal static partial int GameInputInitialize(ref Guid riid, out IntPtr ppv);
}
#endif
