using System.Runtime.InteropServices;
using System.Text;

namespace InputWeave.GameInput;

internal static class NativeUtf8String
{
    /// <summary>
    /// Creates a managed string from a native null-terminated UTF-8 string; the scan length is protected by the
    /// <see cref="NativeSizeGuard.MaxUtf8StringByteLength"/> limit, preventing unbounded scanning and allocation when the native
    /// side lacks a null terminator or reports an abnormally long string.
    /// 從原生 null 結尾 UTF-8 字串建立 managed 字串；掃描長度受
    /// <see cref="NativeSizeGuard.MaxUtf8StringByteLength"/> 上限保護，避免原生端缺少
    /// null 結尾或回報異常長字串時造成無上限的掃描與配置。
    /// </summary>
    /// <param name="pointer">The pointer to the native UTF-8 string. 指向原生 UTF-8 字串的指標。</param>
    /// <returns>The corresponding managed string; returns <c>null</c> when the pointer is <see cref="IntPtr.Zero"/>. 對應的 managed 字串；指標為 <see cref="IntPtr.Zero"/> 時傳回 <c>null</c>。</returns>
    /// <exception cref="InvalidOperationException">The string length exceeds <see cref="NativeSizeGuard.MaxUtf8StringByteLength"/>. 字串長度超過 <see cref="NativeSizeGuard.MaxUtf8StringByteLength"/>。</exception>
    internal static string? FromNullTerminated(IntPtr pointer)
    {
        if (pointer == IntPtr.Zero)
        {
            return null;
        }

        int length = 0;
        while (Marshal.ReadByte(pointer, length) != 0)
        {
            length++;
            if (length > NativeSizeGuard.MaxUtf8StringByteLength)
            {
                throw new InvalidOperationException($"原生回報的 UTF-8 字串長度超過上限（{NativeSizeGuard.MaxUtf8StringByteLength} 位元組），視為裝置或驅動程式回報異常。");
            }
        }

        if (length == 0)
        {
            return string.Empty;
        }

        byte[] bytes = new byte[length];
        Marshal.Copy(pointer, bytes, 0, bytes.Length);
        return Encoding.UTF8.GetString(bytes);
    }
}
