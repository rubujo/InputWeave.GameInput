using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// GameInput 裝置包裝。
/// </summary>
public sealed class GameInputDevice : IDisposable
{
    private IGameInputDevice? _native;
    private bool _disposed;

    internal GameInputDevice(IGameInputDevice native)
    {
        _native = native;
    }

    internal IGameInputDevice NativeInterface
    {
        get
        {
            return Native;
        }
    }

    /// <summary>
    /// 目前裝置狀態。
    /// </summary>
    public GameInputDeviceStatus Status
    {
        get
        {
            return Native.GetDeviceStatus();
        }
    }

    /// <summary>
    /// 取得裝置資訊。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputDeviceInfo GetDeviceInfo()
    {
        int hResult = Native.GetDeviceInfo(out IntPtr info);
        GameInputException.ThrowIfFailed(hResult);
        return Marshal.PtrToStructure<GameInputDeviceInfo>(info);
    }

    /// <summary>
    /// 取得裝置顯示名稱。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public string? GetDisplayName()
    {
        GameInputDeviceInfo info = GetDeviceInfo();
        return info.DisplayName == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(info.DisplayName);
    }

    /// <summary>
    /// 取得裝置 PnP 路徑。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public string? GetPnpPath()
    {
        GameInputDeviceInfo info = GetDeviceInfo();
        return info.PnpPath == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(info.PnpPath);
    }

    /// <summary>
    /// 取得不持有原生指標的裝置資訊快照。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputDeviceInfoSnapshot GetDeviceInfoSnapshot()
    {
        return GameInputDeviceInfoSnapshot.FromNative(GetDeviceInfo());
    }

    /// <summary>
    /// 嘗試取得觸覺資訊。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputHapticInfo? GetHapticInfo()
    {
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<GameInputHapticInfo>());
        try
        {
            int hResult = Native.GetHapticInfo(pointer);
            if (hResult == GameInputHResult.HapticInfoNotFound)
            {
                return null;
            }

            GameInputException.ThrowIfFailed(hResult);
            return Marshal.PtrToStructure<GameInputHapticInfo>(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    /// 嘗試取得不持有原生緩衝區的觸覺資訊快照。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputHapticInfoSnapshot? GetHapticInfoSnapshot()
    {
        GameInputHapticInfo? hapticInfo = GetHapticInfo();
        return hapticInfo.HasValue ? GameInputHapticInfoSnapshot.FromNative(hapticInfo.GetValueOrDefault()) : null;
    }

    /// <summary>
    /// 建立 force feedback effect。
    /// </summary>
    /// <param name="motorIndex">force feedback motor 索引。</param>
    /// <param name="parameters">GameInput 原生參數。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputForceFeedbackEffect CreateForceFeedbackEffect(uint motorIndex, in GameInputForceFeedbackParams parameters)
    {
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<GameInputForceFeedbackParams>());
        try
        {
            Marshal.StructureToPtr(parameters, pointer, fDeleteOld: false);
            int hResult = Native.CreateForceFeedbackEffect(motorIndex, pointer, out IGameInputForceFeedbackEffect? effect);
            GameInputException.ThrowIfFailed(hResult);
            return effect is null
                ? throw new GameInputException(GameInputHResult.FeedbackNotSupported)
                : new GameInputForceFeedbackEffect(effect);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    /// 使用 managed force feedback builder 產生的參數建立 effect。
    /// </summary>
    /// <param name="motorIndex">force feedback motor 索引。</param>
    /// <param name="parameters">GameInput 原生參數。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputForceFeedbackEffect CreateForceFeedbackEffect(uint motorIndex, GameInputForceFeedbackParams parameters)
    {
        return CreateForceFeedbackEffect(motorIndex, in parameters);
    }

    /// <summary>
    /// 指定馬達是否已供電。
    /// </summary>
    /// <param name="motorIndex">force feedback motor 索引。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool IsForceFeedbackMotorPoweredOn(uint motorIndex)
    {
        return Native.IsForceFeedbackMotorPoweredOn(motorIndex);
    }

    /// <summary>
    /// 設定 force feedback 馬達 gain。
    /// </summary>
    /// <param name="motorIndex">force feedback motor 索引。</param>
    /// <param name="masterGain">要套用的 master gain。</param>
    public void SetForceFeedbackMotorGain(uint motorIndex, float masterGain)
    {
        Native.SetForceFeedbackMotorGain(motorIndex, masterGain);
    }

    /// <summary>
    /// 設定震動狀態。
    /// </summary>
    /// <param name="parameters">GameInput 原生參數。</param>
    public void SetRumbleState(in GameInputRumbleParams parameters)
    {
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<GameInputRumbleParams>());
        try
        {
            Marshal.StructureToPtr(parameters, pointer, fDeleteOld: false);
            Native.SetRumbleState(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    /// 清除震動狀態。
    /// </summary>
    public void ClearRumbleState()
    {
        Native.SetRumbleState(IntPtr.Zero);
    }

    /// <summary>
    /// 執行 DirectInput escape。
    /// </summary>
    /// <param name="command">DirectInput escape 命令。</param>
    /// <param name="input">輸入資料緩衝區。</param>
    /// <param name="output">輸出資料緩衝區。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public int DirectInputEscape(uint command, byte[] input, byte[] output)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
#else
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (output is null)
        {
            throw new ArgumentNullException(nameof(output));
        }
#endif

        IntPtr inputPointer = Marshal.AllocHGlobal(input.Length);
        IntPtr outputPointer = Marshal.AllocHGlobal(output.Length);
        try
        {
            Marshal.Copy(input, 0, inputPointer, input.Length);
            int hResult = Native.DirectInputEscape(command, inputPointer, (uint)input.Length, outputPointer, (uint)output.Length, out uint written);
            GameInputException.ThrowIfFailed(hResult);
            int count = checked((int)written);
            Marshal.Copy(outputPointer, output, 0, count);
            return count;
        }
        finally
        {
            Marshal.FreeHGlobal(inputPointer);
            Marshal.FreeHGlobal(outputPointer);
        }
    }

    /// <summary>
    /// 建立 input mapper。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputMapper CreateInputMapper()
    {
        int hResult = Native.CreateInputMapper(out IGameInputMapper? mapper);
        GameInputException.ThrowIfFailed(hResult);
        return mapper is null
            ? throw new GameInputException(GameInputHResult.ObjectNoLongerExists)
            : new GameInputMapper(mapper);
    }

    /// <summary>
    /// 取得額外軸索引。
    /// </summary>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public byte[] GetExtraAxisIndexes(GameInputKind inputKind)
    {
        int hResult = Native.GetExtraAxisCount(inputKind, out uint count);
        GameInputException.ThrowIfFailed(hResult);
        byte[] indexes = new byte[count];
        if (count == 0)
        {
            return indexes;
        }

        hResult = Native.GetExtraAxisIndexes(inputKind, count, indexes);
        GameInputException.ThrowIfFailed(hResult);
        return indexes;
    }

    /// <summary>
    /// 取得額外按鈕索引。
    /// </summary>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public byte[] GetExtraButtonIndexes(GameInputKind inputKind)
    {
        int hResult = Native.GetExtraButtonCount(inputKind, out uint count);
        GameInputException.ThrowIfFailed(hResult);
        byte[] indexes = new byte[count];
        if (count == 0)
        {
            return indexes;
        }

        hResult = Native.GetExtraButtonIndexes(inputKind, count, indexes);
        GameInputException.ThrowIfFailed(hResult);
        return indexes;
    }

    /// <summary>
    /// 建立 raw device report。
    /// </summary>
    /// <param name="reportId">raw device report 識別碼。</param>
    /// <param name="reportKind">raw device report 種類。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputRawDeviceReport CreateRawDeviceReport(uint reportId, GameInputRawDeviceReportKind reportKind)
    {
        int hResult = Native.CreateRawDeviceReport(reportId, reportKind, out IGameInputRawDeviceReport? report);
        GameInputException.ThrowIfFailed(hResult);
        return report is null
            ? throw new GameInputException(GameInputHResult.ObjectNoLongerExists)
            : new GameInputRawDeviceReport(report);
    }

    /// <summary>
    /// 傳送 raw device output。
    /// </summary>
    /// <param name="report">要傳送或操作的 raw device report。</param>
    public void SendRawDeviceOutput(GameInputRawDeviceReport report)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(report);
#else
        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }
#endif

        int hResult = Native.SendRawDeviceOutput(report.NativeInterface);
        GameInputException.ThrowIfFailed(hResult);
    }

    /// <summary>
    /// 釋放此裝置包裝持有的 COM 參考。
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

    private IGameInputDevice Native
    {
        get
        {
            return _disposed
                ? throw new ObjectDisposedException(nameof(GameInputDevice))
                : _native ?? throw new ObjectDisposedException(nameof(GameInputDevice));
        }
    }
}
