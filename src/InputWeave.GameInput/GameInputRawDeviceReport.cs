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
    public GameInputRawDeviceReportInfo GetReportInfo()
    {
        Native.GetReportInfo(out GameInputRawDeviceReportInfo info);
        return info;
    }

    /// <summary>
    /// 取得 raw data 大小。
    /// </summary>
    public int GetRawDataSize()
    {
        return checked((int)Native.GetRawDataSize().ToUInt64());
    }

    /// <summary>
    /// 複製 raw data 到指定緩衝區。
    /// </summary>
    /// <param name="buffer">目的緩衝區。</param>
    /// <returns>實際複製位元組數。</returns>
    public int CopyRawData(byte[] buffer)
    {
        return buffer is null ? throw new ArgumentNullException(nameof(buffer)) : CopyRawData(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// 複製 raw data 到指定陣列區段。
    /// </summary>
    public int CopyRawData(byte[] buffer, int offset, int count)
    {
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

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
    /// <returns>若 GameInput 接受資料，傳回 <see langword="true"/>。</returns>
    public bool SetRawData(byte[] data)
    {
        return data is null ? throw new ArgumentNullException(nameof(data)) : SetRawData(data, 0, data.Length);
    }

    /// <summary>
    /// 設定 raw data。
    /// </summary>
    public bool SetRawData(byte[] data, int offset, int count)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

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
        public int CopyRawData(Span<byte> buffer)
        {
            byte[] managedBuffer = new byte[buffer.Length];
            int count = CopyRawData(managedBuffer);
            managedBuffer.AsSpan(0, count).CopyTo(buffer);
            return count;
        }

        /// <summary>
        /// 從 span 設定 raw data。
        /// </summary>
        public bool SetRawData(ReadOnlySpan<byte> data)
        {
            return SetRawData(data.ToArray());
        }
#endif

    /// <summary>
    /// 取得此 report 所屬裝置。
    /// </summary>
    public GameInputDevice? GetDevice()
    {
        Native.GetDevice(out IGameInputDevice? device);
        return device is null ? null : new GameInputDevice(device);
    }

    /// <inheritdoc />
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
