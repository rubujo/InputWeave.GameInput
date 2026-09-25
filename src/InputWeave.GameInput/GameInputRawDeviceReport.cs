using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// A GameInput raw device report wrapper.
/// GameInput raw device report 包裝。
/// </summary>
public sealed class GameInputRawDeviceReport : IDisposable
{
    /// <summary>
    /// The maximum native-reported size, in bytes, accepted by <see cref="GetRawDataSize"/>.
    /// <see cref="GetRawDataSize"/> 接受的原生回報大小上限（位元組）。
    /// </summary>
    /// <remarks>
    /// Actual HID/GameInput raw reports are usually far smaller than this limit; exceeding it is treated as an anomalous device
    /// or driver report and the corresponding memory allocation is refused, preventing a single anomalous device from causing an
    /// oversized allocation.
    /// 實際 HID／GameInput raw report 通常遠小於這個上限；超過時視為裝置或驅動程式回報異常，
    /// 拒絕配置對應大小的記憶體，避免單一異常裝置造成過大的配置。
    /// </remarks>
    public const int MaxRawDataSize = 64 * 1024;

    private readonly GameInputComHandle _handle;

    internal GameInputRawDeviceReport(IGameInputRawDeviceReport native)
    {
        _handle = new GameInputComHandle(native.Pointer);
    }

    internal IGameInputRawDeviceReport NativeInterface
    {
        get
        {
            return _handle.IsClosed
                ? throw new ObjectDisposedException(nameof(GameInputRawDeviceReport))
                : new IGameInputRawDeviceReport(_handle.DangerousGetHandle());
        }
    }

    /// <summary>
    /// Gets the report information.
    /// 取得 report 資訊。
    /// </summary>
    /// <returns>The raw device report information. Raw device report 資訊。</returns>
    public GameInputRawDeviceReportInfo GetReportInfo()
    {
        using ComLease<IGameInputRawDeviceReport> call = EnterNative();
        call.Native.GetReportInfo(out GameInputRawDeviceReportInfo info);
        return info;
    }

    /// <summary>
    /// Gets the raw data size.
    /// 取得 raw data 大小。
    /// </summary>
    /// <remarks>
    /// When the native-reported size exceeds <see cref="MaxRawDataSize"/>, it is treated as an anomalous device or driver report
    /// and an <see cref="InvalidOperationException"/> is thrown instead of attempting to allocate memory of that size.
    /// 若原生回報的大小超過 <see cref="MaxRawDataSize"/>，視為裝置或驅動程式回報異常，拋出
    /// <see cref="InvalidOperationException"/> 而不是嘗試配置對應大小的記憶體。
    /// </remarks>
    /// <returns>The raw data size in bytes. Raw data 大小（位元組）。</returns>
    /// <exception cref="InvalidOperationException">The size reported by the native side exceeds <see cref="MaxRawDataSize"/>. 原生回報的大小超過 <see cref="MaxRawDataSize"/>。</exception>
    public int GetRawDataSize()
    {
        using ComLease<IGameInputRawDeviceReport> call = EnterNative();
        return NativeSizeGuard.EnsureCount(call.Native.GetRawDataSize().ToUInt64(), MaxRawDataSize, "raw device report 大小（位元組）");
    }

    /// <summary>
    /// Copies the raw data into the specified buffer.
    /// 複製 raw data 到指定緩衝區。
    /// </summary>
    /// <param name="buffer">The destination data buffer. 目標資料緩衝區。</param>
    /// <returns>The number of bytes actually copied. 實際複製的位元組數。</returns>
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
    /// Copies the raw data into the specified array segment.
    /// 複製 raw data 到指定陣列區段。
    /// </summary>
    /// <param name="buffer">The destination data buffer. 目標資料緩衝區。</param>
    /// <param name="offset">The starting offset within the data buffer. 資料緩衝區起始位移。</param>
    /// <param name="count">The number of bytes to read or write. 要讀寫的位元組數。</param>
    /// <returns>The number of bytes actually copied. 實際複製的位元組數。</returns>
    public unsafe int CopyRawData(byte[] buffer, int offset, int count)
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

        // 直接釘選呼叫端陣列的指定區段交給原生端寫入，不經暫存原生緩衝區與額外複製。
        // 空陣列經 fixed 會得到空指標；count 為 0 時改傳有效的堆疊位址，維持原本「永遠傳入有效指標」的行為。
        byte placeholder = 0;
        fixed (byte* start = buffer)
        {
            byte* destination = count == 0 ? &placeholder : start + offset;
            using ComLease<IGameInputRawDeviceReport> call = EnterNative();
            UIntPtr written = call.Native.GetRawData((UIntPtr)count, (IntPtr)destination);
            return EnsureNativeWrittenCount(written.ToUInt64(), count, "raw device report 複製位元組數");
        }
    }

    /// <summary>
    /// Gets a new array of the raw data.
    /// 取得 raw data 的新陣列。
    /// </summary>
    /// <returns>A new array containing the raw data. 包含 raw data 的新陣列。</returns>
    /// <exception cref="InvalidOperationException">The size reported by the native side exceeds <see cref="MaxRawDataSize"/> (thrown by <see cref="GetRawDataSize"/>). 原生回報的大小超過 <see cref="MaxRawDataSize"/>（由 <see cref="GetRawDataSize"/> 拋出）。</exception>
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
    /// Sets the raw data.
    /// 設定 raw data。
    /// </summary>
    /// <param name="data">The data to write. 要寫入的資料。</param>
    /// <returns>Returns true when the raw data was applied; otherwise returns false. Raw data 設定成功時傳回 true；否則傳回 false。</returns>
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
    /// Sets the raw data.
    /// 設定 raw data。
    /// </summary>
    /// <param name="data">The data to write. 要寫入的資料。</param>
    /// <param name="offset">The starting offset within the data buffer. 資料緩衝區起始位移。</param>
    /// <param name="count">The number of bytes to read or write. 要讀寫的位元組數。</param>
    /// <returns>Returns true when the raw data was applied; otherwise returns false. Raw data 設定成功時傳回 true；否則傳回 false。</returns>
    public unsafe bool SetRawData(byte[] data, int offset, int count)
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

        byte placeholder = 0;
        fixed (byte* start = data)
        {
            byte* source = count == 0 ? &placeholder : start + offset;
            using ComLease<IGameInputRawDeviceReport> call = EnterNative();
            return call.Native.SetRawData((UIntPtr)count, (IntPtr)source);
        }
    }

#if NET10_0_OR_GREATER
        /// <summary>
        /// Copies the raw data into the specified span.
        /// 複製 raw data 到指定 span。
        /// </summary>
        /// <param name="buffer">The destination data buffer. 目標資料緩衝區。</param>
        /// <returns>The number of bytes actually copied. 實際複製的位元組數。</returns>
        public unsafe int CopyRawData(Span<byte> buffer)
        {
            // 與陣列多載一致：空的 span 經 fixed 會得到空指標，改傳有效的堆疊位址。
            byte placeholder = 0;
            fixed (byte* start = buffer)
            {
                byte* pointer = buffer.IsEmpty ? &placeholder : start;
                using ComLease<IGameInputRawDeviceReport> call = EnterNative();
                UIntPtr written = call.Native.GetRawData((UIntPtr)buffer.Length, (IntPtr)pointer);
                return EnsureNativeWrittenCount(written.ToUInt64(), buffer.Length, "raw device report 複製位元組數");
            }
        }

        /// <summary>
        /// Sets the raw data from a span.
        /// 從 span 設定 raw data。
        /// </summary>
        /// <param name="data">The data to write. 要寫入的資料。</param>
        /// <returns>Returns true when the raw data was applied; otherwise returns false. Raw data 設定成功時傳回 true；否則傳回 false。</returns>
        public unsafe bool SetRawData(ReadOnlySpan<byte> data)
        {
            byte placeholder = 0;
            fixed (byte* start = data)
            {
                byte* pointer = data.IsEmpty ? &placeholder : start;
                using ComLease<IGameInputRawDeviceReport> call = EnterNative();
                return call.Native.SetRawData((UIntPtr)data.Length, (IntPtr)pointer);
            }
        }
#endif

    /// <summary>
    /// Gets the device that owns this report.
    /// 取得此 report 所屬裝置。
    /// </summary>
    /// <returns>The owning device wrapper, or null when unavailable. 所屬裝置包裝；無法取得時為 null。</returns>
    public GameInputDevice? GetDevice()
    {
        using ComLease<IGameInputRawDeviceReport> call = EnterNative();
        call.Native.GetDevice(out IGameInputDevice? device);
        return device is { } deviceValue ? new GameInputDevice(deviceValue) : null;
    }

    /// <summary>
    /// Releases the COM reference held by the raw device report wrapper.
    /// 釋放 raw device report 包裝持有的 COM 參考。
    /// </summary>
    public void Dispose()
    {
        // SafeHandle.Dispose 本身冪等且執行緒安全，重複或並行呼叫只會釋放一次。
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }

    internal ComLease<IGameInputRawDeviceReport> EnterNative()
    {
        return _handle.Acquire<IGameInputRawDeviceReport>(nameof(GameInputRawDeviceReport));
    }

    internal static int EnsureNativeWrittenCount(ulong written, int capacity, string subject)
    {
        if (written > (ulong)capacity)
        {
            throw new InvalidOperationException($"{subject}（{written}）超過呼叫端提供的緩衝區大小（{capacity}）。");
        }

        return checked((int)written);
    }
}
