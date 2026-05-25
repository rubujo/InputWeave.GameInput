#if NETFRAMEWORK
using System;

namespace InputWeave.GameInput.Interop;

internal static partial class GameInputNativeMethods
{
    internal static int GameInputInitialize(ref Guid riid, out IntPtr ppv)
    {
        return InputWeave.GameInput.GameInputRuntimeLoader.GameInputInitialize(ref riid, out ppv);
    }
}
#endif
