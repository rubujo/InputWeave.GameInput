using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// GameInput raw device report 包裝。
/// </summary>
public sealed class GameInputRawDeviceReport : IDisposable
{
    private IGameInputRawDeviceReport? _native;
    private bool _disposed;

    internal GameInputRawDeviceReport(IGameInputRawDeviceReport native)
    {
        _native = native;
    }

    internal IGameInputRawDeviceReport NativeInterface
    {
        get
        {
            return Native;
        }
    }

    /// <summary>
    /// 取得 report 資訊。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputRawDeviceReportInfo GetReportInfo()
    {
        Native.GetReportInfo(out GameInputRawDeviceReportInfo info);
        return info;
    }

    /// <summary>
    /// 取得 raw data 大小。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public int GetRawDataSize()
    {
        return checked((int)Native.GetRawDataSize().ToUInt64());
    }

    /// <summary>
    /// 複製 raw data 到指定緩衝區。
    /// </summary>
    /// <param name="buffer">目標資料緩衝區。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public int CopyRawData(byte[] buffer)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(buffer);
#else
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }
#endif

        return CopyRawData(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// 複製 raw data 到指定陣列區段。
    /// </summary>
    /// <param name="buffer">目標資料緩衝區。</param>
    /// <param name="offset">資料緩衝區起始位移。</param>
    /// <param name="count">要讀寫的位元組數。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public int CopyRawData(byte[] buffer, int offset, int count)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(buffer);
#else
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }
#endif

        if (offset < 0 || count < 0 || offset > buffer.Length - count)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "指定的 raw data 緩衝區區段超出陣列範圍。");
        }

        IntPtr nativeBuffer = Marshal.AllocHGlobal(count);
        try
        {
            UIntPtr written = Native.GetRawData((UIntPtr)count, nativeBuffer);
            int writtenCount = checked((int)written.ToUInt64());
            Marshal.Copy(nativeBuffer, buffer, offset, writtenCount);
            return writtenCount;
        }
        finally
        {
            Marshal.FreeHGlobal(nativeBuffer);
        }
    }

    /// <summary>
    /// 取得 raw data 的新陣列。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public byte[] GetRawData()
    {
        byte[] buffer = new byte[GetRawDataSize()];
        int count = CopyRawData(buffer);
        if (count == buffer.Length)
        {
            return buffer;
        }

        Array.Resize(ref buffer, count);
        return buffer;
    }

    /// <summary>
    /// 設定 raw data。
    /// </summary>
    /// <param name="data">要寫入的資料。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool SetRawData(byte[] data)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(data);
#else
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }
#endif

        return SetRawData(data, 0, data.Length);
    }

    /// <summary>
    /// 設定 raw data。
    /// </summary>
    /// <param name="data">要寫入的資料。</param>
    /// <param name="offset">資料緩衝區起始位移。</param>
    /// <param name="count">要讀寫的位元組數。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool SetRawData(byte[] data, int offset, int count)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(data);
#else
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }
#endif

        if (offset < 0 || count < 0 || offset > data.Length - count)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "指定的 raw data 區段超出陣列範圍。");
        }

        IntPtr nativeBuffer = Marshal.AllocHGlobal(count);
        try
        {
            Marshal.Copy(data, offset, nativeBuffer, count);
            return Native.SetRawData((UIntPtr)count, nativeBuffer);
        }
        finally
        {
            Marshal.FreeHGlobal(nativeBuffer);
        }
    }

#if NET10_0_OR_GREATER
        /// <summary>
        /// 複製 raw data 到指定 span。
        /// </summary>
        /// <param name="buffer">目標資料緩衝區。</param>
        /// <returns>操作完成後的查詢或建立結果。</returns>
        public unsafe int CopyRawData(Span<byte> buffer)
        {
            fixed (byte* pointer = buffer)
            {
                UIntPtr written = Native.GetRawData((UIntPtr)buffer.Length, (IntPtr)pointer);
                return checked((int)written.ToUInt64());
            }
        }

        /// <summary>
        /// 從 span 設定 raw data。
        /// </summary>
        /// <param name="data">要寫入的資料。</param>
        /// <returns>操作完成後的查詢或建立結果。</returns>
        public unsafe bool SetRawData(ReadOnlySpan<byte> data)
        {
            fixed (byte* pointer = data)
            {
                return Native.SetRawData((UIntPtr)data.Length, (IntPtr)pointer);
            }
        }
#endif

    /// <summary>
    /// 取得此 report 所屬裝置。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputDevice? GetDevice()
    {
        Native.GetDevice(out IGameInputDevice? device);
        return device is null ? null : new GameInputDevice(device);
    }

    /// <summary>
    /// 釋放 raw device report 包裝持有的 COM 參考。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_native is not null)
        {
            Marshal.ReleaseComObject(_native);
            _native = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private IGameInputRawDeviceReport Native
    {
        get
        {
            return _disposed
                ? throw new ObjectDisposedException(nameof(GameInputRawDeviceReport))
                : _native ?? throw new ObjectDisposedException(nameof(GameInputRawDeviceReport));
        }
    }
}
