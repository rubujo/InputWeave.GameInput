using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
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
    /// 依指定 quota dispatch callback。
    /// </summary>
    /// <param name="quotaInMicroseconds">dispatcher 可使用的時間配額，單位為微秒。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool Dispatch(ulong quotaInMicroseconds)
    {
        return Native.Dispatch(quotaInMicroseconds);
    }

    /// <summary>
    /// 開啟 dispatcher wait handle。
    /// </summary>
    /// <remarks>
    /// 傳回的原生 handle 由呼叫端負責關閉；本函式庫沒有公開對應的關閉 API。一般情境應改用
    /// <see cref="OpenSafeWaitHandle"/>，讓 <see cref="GameInputDispatcherWaitHandle"/> 以 <see cref="SafeHandle"/>
    /// 管理 handle 生命週期，避免忘記關閉造成 handle 洩漏。
    /// </remarks>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public IntPtr OpenWaitHandle()
    {
        int hResult = Native.OpenWaitHandle(out IntPtr waitHandle);
        GameInputException.ThrowIfFailed(hResult);
        return waitHandle;
    }

    /// <summary>
    /// 開啟 dispatcher wait handle，並由 managed wrapper 負責關閉 handle。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputDispatcherWaitHandle OpenSafeWaitHandle()
    {
        IntPtr waitHandle = OpenWaitHandle();
        return new GameInputDispatcherWaitHandle(waitHandle);
    }

    /// <summary>
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
            Marshal.ReleaseComObject(_native);
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
