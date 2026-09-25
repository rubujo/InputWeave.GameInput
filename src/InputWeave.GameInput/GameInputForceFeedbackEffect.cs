using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// A GameInput force feedback effect wrapper.
/// GameInput force feedback effect 包裝。
/// </summary>
public sealed class GameInputForceFeedbackEffect : IDisposable
{
    private readonly GameInputComHandle _handle;
    private int _disposed;

    internal GameInputForceFeedbackEffect(IGameInputForceFeedbackEffect native)
    {
        _handle = new GameInputComHandle(native.Pointer);
    }

    /// <summary>
    /// The motor index.
    /// 馬達索引。
    /// </summary>
    public uint MotorIndex
    {
        get
        {
            using ComLease<IGameInputForceFeedbackEffect> call = EnterNative();
            return call.Native.GetMotorIndex();
        }
    }

    /// <summary>
    /// effect gain。
    /// </summary>
    public float Gain
    {
        get
        {
            using ComLease<IGameInputForceFeedbackEffect> call = EnterNative();
            return call.Native.GetGain();
        }

        set
        {
            using ComLease<IGameInputForceFeedbackEffect> call = EnterNative();
            call.Native.SetGain(value);
        }
    }

    /// <summary>
    /// The effect state.
    /// effect 狀態。
    /// </summary>
    public GameInputFeedbackEffectState State
    {
        get
        {
            using ComLease<IGameInputForceFeedbackEffect> call = EnterNative();
            return call.Native.GetState();
        }

        set
        {
            using ComLease<IGameInputForceFeedbackEffect> call = EnterNative();
            call.Native.SetState(value);
        }
    }

    /// <summary>
    /// Gets the effect parameters.
    /// 取得 effect 參數。
    /// </summary>
    /// <returns>The current force feedback effect parameters. 目前的 force feedback effect 參數。</returns>
    public GameInputForceFeedbackParams GetParams()
    {
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<GameInputForceFeedbackParams>());
        try
        {
            using ComLease<IGameInputForceFeedbackEffect> call = EnterNative();
            call.Native.GetParams(pointer);
            return Marshal.PtrToStructure<GameInputForceFeedbackParams>(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    /// Sets the effect parameters.
    /// 設定 effect 參數。
    /// </summary>
    /// <param name="parameters">The native GameInput parameters. GameInput 原生參數。</param>
    /// <returns>Returns true when the parameters were applied; otherwise returns false. 參數套用成功時傳回 true；否則傳回 false。</returns>
    public bool SetParams(in GameInputForceFeedbackParams parameters)
    {
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<GameInputForceFeedbackParams>());
        try
        {
            Marshal.StructureToPtr(parameters, pointer, fDeleteOld: false);
            using ComLease<IGameInputForceFeedbackEffect> call = EnterNative();
            return call.Native.SetParams(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    /// Gets the device that owns this effect.
    /// 取得此 effect 所屬裝置。
    /// </summary>
    /// <returns>The owning device wrapper, or null when unavailable. 所屬裝置包裝；無法取得時為 null。</returns>
    public GameInputDevice? GetDevice()
    {
        using ComLease<IGameInputForceFeedbackEffect> call = EnterNative();
        call.Native.GetDevice(out IGameInputDevice? device);
        return device is { } deviceValue ? new GameInputDevice(deviceValue) : null;
    }

    /// <summary>
    /// Releases the COM reference held by the force feedback effect wrapper.
    /// 釋放 force feedback effect 包裝持有的 COM 參考。
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        _handle.Dispose();

        GC.SuppressFinalize(this);
    }

    private ComLease<IGameInputForceFeedbackEffect> EnterNative()
    {
        return Volatile.Read(ref _disposed) != 0
            ? throw new ObjectDisposedException(nameof(GameInputForceFeedbackEffect))
            : _handle.Acquire(static pointer => new IGameInputForceFeedbackEffect(pointer), nameof(GameInputForceFeedbackEffect));
    }
}
