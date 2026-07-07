using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// A GameInput callback dispatcher wrapper.
/// GameInput callback dispatcher 包裝。
/// </summary>
public sealed class GameInputDispatcher : IDisposable
{
    private IGameInputDispatcher? _native;
    private bool _disposed;

    internal GameInputDispatcher(IGameInputDispatcher native)
    {
        _native = native;
    }

    /// <summary>
    /// Dispatches callbacks within the specified quota.
    /// 依指定 quota dispatch callback。
    /// </summary>
    /// <param name="quotaInMicroseconds">The time quota available to the dispatcher, in microseconds. dispatcher 可使用的時間配額，單位為微秒。</param>
    /// <returns>Returns true when more callback work remains after the quota is consumed; otherwise returns false. 配額用完後仍有回呼工作待處理時傳回 true；否則傳回 false。</returns>
    public bool Dispatch(ulong quotaInMicroseconds)
    {
        return Native.Dispatch(quotaInMicroseconds);
    }

    /// <summary>
    /// Opens the dispatcher wait handle.
    /// 開啟 dispatcher wait handle。
    /// </summary>
    /// <remarks>
    /// The returned native handle must be closed by the caller; this library does not expose a corresponding close API. In
    /// typical scenarios use <see cref="OpenSafeWaitHandle"/> instead, letting <see cref="GameInputDispatcherWaitHandle"/> manage
    /// the handle lifetime with <see cref="SafeHandle"/> to avoid handle leaks from forgetting to close it.
    /// 傳回的原生 handle 由呼叫端負責關閉；本函式庫沒有公開對應的關閉 API。一般情境應改用
    /// <see cref="OpenSafeWaitHandle"/>，讓 <see cref="GameInputDispatcherWaitHandle"/> 以 <see cref="SafeHandle"/>
    /// 管理 handle 生命週期，避免忘記關閉造成 handle 洩漏。
    /// </remarks>
    /// <returns>The native wait handle; the caller is responsible for closing it. 原生 wait handle；由呼叫端負責關閉。</returns>
    public IntPtr OpenWaitHandle()
    {
        int hResult = Native.OpenWaitHandle(out IntPtr waitHandle);
        GameInputException.ThrowIfFailed(hResult);
        return waitHandle;
    }

    /// <summary>
    /// Opens the dispatcher wait handle with a managed wrapper responsible for closing it.
    /// 開啟 dispatcher wait handle，並由 managed wrapper 負責關閉 handle。
    /// </summary>
    /// <returns>The managed wrapper that owns the wait handle lifetime. 擁有 wait handle 生命週期的 managed 包裝。</returns>
    public GameInputDispatcherWaitHandle OpenSafeWaitHandle()
    {
        IntPtr waitHandle = OpenWaitHandle();
        return new GameInputDispatcherWaitHandle(waitHandle);
    }

    /// <summary>
    /// Releases the COM reference held by the dispatcher wrapper.
    /// 釋放 dispatcher 包裝持有的 COM 參考。
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

    private IGameInputDispatcher Native
    {
        get
        {
            return _disposed
                ? throw new ObjectDisposedException(nameof(GameInputDispatcher))
                : _native ?? throw new ObjectDisposedException(nameof(GameInputDispatcher));
        }
    }
}
