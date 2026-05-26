using System.Runtime.InteropServices;
#if !NET5_0_OR_GREATER
using System.Text;
#endif

namespace InputWeave.GameInput;

internal static class NativeUtf8String
{
    internal static string? FromNullTerminated(IntPtr pointer)
    {
        if (pointer == IntPtr.Zero)
        {
            return null;
        }

#if NET5_0_OR_GREATER
        return Marshal.PtrToStringUTF8(pointer);
#else
        int length = 0;
        while (Marshal.ReadByte(pointer, length) != 0)
        {
            length++;
        }

        if (length == 0)
        {
            return string.Empty;
        }

        byte[] bytes = new byte[length];
        Marshal.Copy(pointer, bytes, 0, bytes.Length);
        return Encoding.UTF8.GetString(bytes);
#endif
    }
}
