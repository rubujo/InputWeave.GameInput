using System.Runtime.InteropServices;

namespace InputWeave.GameInput;

/// <summary>
/// A native GameInput callback registration.
/// GameInput 原生回呼註冊。
/// </summary>
public sealed class GameInputCallbackRegistration : IDisposable
{
    private readonly Func<ulong, bool> _unregisterCallback;
    private readonly Action<GameInputCallbackRegistration> _removeRegistration;
    private readonly Action _deactivateContext;
    private readonly Func<Action?> _acquireOwnerLease;
    private GCHandle _contextHandle;
    private int _disposed;

    internal GameInputCallbackRegistration(
        ulong token,
        GCHandle contextHandle,
        Action deactivateContext,
        Func<ulong, bool> unregisterCallback,
        Action<GameInputCallbackRegistration> removeRegistration,
        Func<Action?>? acquireOwnerLease = null)
    {
        Token = token;
        _contextHandle = contextHandle;
        _deactivateContext = deactivateContext;
        _unregisterCallback = unregisterCallback;
        _removeRegistration = removeRegistration;
        _acquireOwnerLease = acquireOwnerLease ?? (static () => null);
    }

    /// <summary>
    /// The native GameInput callback token.
    /// 原生 GameInput callback token。
    /// </summary>
    public ulong Token { get; }

    /// <summary>
    /// Whether the registration has been disposed.
    /// 註冊是否已釋放。
    /// </summary>
    public bool IsDisposed
    {
        get
        {
            return Volatile.Read(ref _disposed) != 0;
        }
    }

    /// <summary>
    /// Whether the callback context handle has been freed; stays false when <c>UnregisterCallback</c> did not succeed.
    /// 回呼內容的 GCHandle 是否已釋放；<c>UnregisterCallback</c> 未成功時維持 false。
    /// </summary>
    internal bool IsContextHandleReleased
    {
        get
        {
            return !_contextHandle.IsAllocated;
        }
    }

    /// <summary>
    /// Unregisters the callback and releases the related managed state.
    /// 取消註冊 callback 並釋放相關 managed 狀態。
    /// </summary>
    /// <exception cref="InvalidOperationException">This method was called synchronously on the native callback thread. 在原生回呼執行緒中同步呼叫此方法。</exception>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (GameInputCallbackThread.IsExecutingCallback)
        {
            throw new InvalidOperationException("不允許在原生 GameInput 回呼執行緒中同步取消註冊該回呼，這會觸發原生端的致命判斷提示。請改由其他執行緒（例如透過 Task.Run）非同步釋放此註冊。");
        }

        // 回呼執行緒檢查必須在取得釋放權之前，讓被拒絕的呼叫不會把註冊標成已釋放。
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        _deactivateContext();

        bool unregistered = Token == 0;
        try
        {
            if (Token != 0)
            {
                // 刻意不先呼叫 StopCallback：它會非同步移除註冊，讓隨後的 UnregisterCallback 找不到 token 而傳回 false，
                // 導致 GCHandle 無法釋放。UnregisterCallback 本身就保證不再派送，並等待進行中的回呼結束。
                unregistered = _unregisterCallback(Token);
            }
        }
        finally
        {
            // 官方文件：UnregisterCallback 成功返回前，釋放回呼相關資源並不安全。解除註冊失敗或拋出例外時，
            // 保留已停用的 context（處理常式已放掉，只洩漏一個小物件），避免原生端仍在進行的回呼存取已釋放的 GCHandle。
            if (unregistered && _contextHandle.IsAllocated)
            {
                _contextHandle.Free();
            }

            _removeRegistration(this);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// The safe disposal method for internal library use: disposes synchronously via <see cref="Dispose"/> when not on the native
    /// callback thread. On the native callback thread it deactivates the handler immediately and unregisters on a background
    /// thread without waiting, because <c>UnregisterCallback</c> only returns once no callback is running, so waiting here would
    /// deadlock (verified on real hardware). A lease on the owning client is taken on the calling thread and released after the
    /// background unregistration, so the native root object stays alive until then.
    /// 供程式庫內部呼叫的安全釋放方法：不在原生回呼執行緒中時直接同步 <see cref="Dispose"/>。在原生回呼執行緒中時，
    /// 會立即停用處理常式，並在背景執行緒解除註冊而不等待——<c>UnregisterCallback</c> 要等沒有回呼在執行時才會返回，
    /// 在此等待會造成死結（已以實機驗證）。擁有者用戶端的租約在呼叫端執行緒上取得、背景解除註冊後才歸還，
    /// 確保原生根物件在那之前保持存活。
    /// </summary>
    /// <returns>The exception raised during synchronous disposal; <c>null</c> when disposal completed or was deferred. 同步釋放時發生的例外；順利完成或已延後處理時為 <c>null</c>。</returns>
    internal Exception? DisposeSafely()
    {
        if (!GameInputCallbackThread.IsExecutingCallback)
        {
            try
            {
                Dispose();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        DisposeInBackground();
        return null;
    }

    /// <summary>
    /// Deactivates the handler immediately and unregisters on a background thread without waiting, from any thread. Used when
    /// the caller holds a lock that a running callback may also need, because waiting for <c>UnregisterCallback</c> there would
    /// deadlock. A lease on the owning client is taken on the calling thread and released after the background unregistration.
    /// 在任何執行緒上立即停用處理常式，並在背景執行緒解除註冊而不等待。用於呼叫端持有進行中回呼也可能需要的鎖時，
    /// 因為在那裡等待 <c>UnregisterCallback</c> 會造成死結。擁有者用戶端的租約在呼叫端執行緒上取得、背景解除註冊後才歸還。
    /// </summary>
    internal void DisposeInBackground()
    {
        if (IsDisposed)
        {
            return;
        }

        _deactivateContext();
        Action? releaseOwnerLease = _acquireOwnerLease();
        ThreadPool.QueueUserWorkItem(static state =>
        {
            (GameInputCallbackRegistration registration, Action? release) = ((GameInputCallbackRegistration, Action?))state!;
            try
            {
                registration.Dispose();
            }
            catch (Exception ex)
            {
                GameInputClient.RaiseUnhandledCallbackException(ex);
            }
            finally
            {
                release?.Invoke();
            }
        }, (this, releaseOwnerLease));
    }
}
