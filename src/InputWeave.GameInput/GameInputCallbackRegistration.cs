using System.Runtime.InteropServices;

namespace InputWeave.GameInput;

/// <summary>
/// A native GameInput callback registration.
/// GameInput 原生回呼註冊。
/// </summary>
public sealed class GameInputCallbackRegistration : IDisposable
{
    private readonly Action<ulong> _stopCallback;
    private readonly Func<ulong, bool> _unregisterCallback;
    private readonly Action<GameInputCallbackRegistration> _removeRegistration;
    private readonly Action _deactivateContext;
    private GCHandle _contextHandle;

    internal GameInputCallbackRegistration(
        ulong token,
        GCHandle contextHandle,
        Action deactivateContext,
        Action<ulong> stopCallback,
        Func<ulong, bool> unregisterCallback,
        Action<GameInputCallbackRegistration> removeRegistration)
    {
        Token = token;
        _contextHandle = contextHandle;
        _deactivateContext = deactivateContext;
        _stopCallback = stopCallback;
        _unregisterCallback = unregisterCallback;
        _removeRegistration = removeRegistration;
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
    public bool IsDisposed { get; private set; }

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

        IsDisposed = true;
        _deactivateContext();

        try
        {
            if (Token != 0)
            {
                _stopCallback(Token);
                _unregisterCallback(Token);
            }
        }
        finally
        {
            if (_contextHandle.IsAllocated)
            {
                _contextHandle.Free();
            }

            _removeRegistration(this);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// The safe disposal method for internal library use: disposes synchronously via <see cref="Dispose"/> when not on the native
    /// callback thread; when on the native callback thread, invokes <see cref="Dispose"/> on a background thread and waits
    /// synchronously for it to finish, so the caller knows the registration has truly been torn down on return instead of
    /// possibly still pending like a fire-and-forget deferral.
    /// 供程式庫內部呼叫的安全釋放方法：不在原生回呼執行緒中時直接同步 <see cref="Dispose"/>；
    /// 在原生回呼執行緒中時，改在背景執行緒呼叫 <see cref="Dispose"/> 並同步等待其完成，
    /// 讓呼叫端可以確定回傳時這個註冊已經真正解除，而不是像 fire-and-forget 延後那樣可能還沒完成。
    /// </summary>
    /// <returns>The exception raised during disposal; <c>null</c> when disposal completed successfully. 釋放過程中發生的例外；順利完成時為 <c>null</c>。</returns>
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

        using ManualResetEventSlim completed = new(initialState: false);
        Exception? deferredException = null;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                Dispose();
            }
            catch (Exception ex)
            {
                deferredException = ex;
            }
            finally
            {
                completed.Set();
            }
        });

        completed.Wait();
        return deferredException;
    }
}
