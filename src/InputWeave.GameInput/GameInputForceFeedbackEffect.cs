using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// GameInput force feedback effect 包裝。
/// </summary>
public sealed class GameInputForceFeedbackEffect : IDisposable
{
    private IGameInputForceFeedbackEffect? _native;
    private bool _disposed;

    internal GameInputForceFeedbackEffect(IGameInputForceFeedbackEffect native)
    {
        _native = native;
    }

    /// <summary>
    /// 馬達索引。
    /// </summary>
    public uint MotorIndex
    {
        get
        {
            return Native.GetMotorIndex();
        }
    }

    /// <summary>
    /// effect gain。
    /// </summary>
    public float Gain
    {
        get
        {
            return Native.GetGain();
        }

        set
        {
            Native.SetGain(value);
        }
    }

    /// <summary>
    /// effect 狀態。
    /// </summary>
    public GameInputFeedbackEffectState State
    {
        get
        {
            return Native.GetState();
        }

        set
        {
            Native.SetState(value);
        }
    }

    /// <summary>
    /// 取得 effect 參數。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputForceFeedbackParams GetParams()
    {
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<GameInputForceFeedbackParams>());
        try
        {
            Native.GetParams(pointer);
            return Marshal.PtrToStructure<GameInputForceFeedbackParams>(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    /// 設定 effect 參數。
    /// </summary>
    /// <param name="parameters">GameInput 原生參數。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool SetParams(in GameInputForceFeedbackParams parameters)
    {
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<GameInputForceFeedbackParams>());
        try
        {
            Marshal.StructureToPtr(parameters, pointer, fDeleteOld: false);
            return Native.SetParams(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    /// 取得此 effect 所屬裝置。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputDevice? GetDevice()
    {
        Native.GetDevice(out IGameInputDevice? device);
        return device is { } deviceValue ? new GameInputDevice(deviceValue) : null;
    }

    /// <summary>
    /// 釋放 force feedback effect 包裝持有的 COM 參考。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_native is not null)
        {
#if NET10_0_OR_GREATER
            _native.Value.Release();
#else
            Marshal.ReleaseComObject(_native);
#endif
            _native = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private IGameInputForceFeedbackEffect Native
    {
        get
        {
            return _disposed
                ? throw new ObjectDisposedException(nameof(GameInputForceFeedbackEffect))
                : _native ?? throw new ObjectDisposedException(nameof(GameInputForceFeedbackEffect));
        }
    }
}
