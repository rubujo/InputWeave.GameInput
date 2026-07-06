namespace InputWeave.GameInput;

/// <summary>
/// 統一管理一次性原生回呼註冊與取消語彙註冊的指派/釋放順序，
/// 避免「回呼先觸發、區域變數賦值後發生」造成的競爭與資源洩漏。
/// </summary>
internal sealed class DeferredCallbackCompletion
{
#if NET10_0_OR_GREATER
    private readonly System.Threading.Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private GameInputCallbackRegistration? _registration;
    private CancellationTokenRegistration? _cancellationRegistration;
    private bool _completed;

    public void SetRegistration(GameInputCallbackRegistration registration)
    {
        bool alreadyCompleted;
        lock (_lock)
        {
            alreadyCompleted = _completed;
            if (!alreadyCompleted)
            {
                _registration = registration;
            }
        }

        if (alreadyCompleted)
        {
            DisposeDeferred(registration);
        }
    }

    public void SetCancellationRegistration(CancellationTokenRegistration cancellationRegistration)
    {
        bool alreadyCompleted;
        lock (_lock)
        {
            alreadyCompleted = _completed;
            if (!alreadyCompleted)
            {
                _cancellationRegistration = cancellationRegistration;
            }
        }

        if (alreadyCompleted)
        {
            cancellationRegistration.Dispose();
        }
    }

    public void Complete()
    {
        GameInputCallbackRegistration? registration;
        CancellationTokenRegistration? cancellationRegistration;
        lock (_lock)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            registration = _registration;
            cancellationRegistration = _cancellationRegistration;
            _registration = null;
            _cancellationRegistration = null;
        }

        cancellationRegistration?.Dispose();
        if (registration is not null)
        {
            DisposeDeferred(registration);
        }
    }

    /// <summary>
    /// 只釋放目前已知的取消語彙註冊，不影響完成狀態或原生回呼註冊；可安全重複呼叫。
    /// </summary>
    public void DisposeCancellationRegistrationForDispose()
    {
        CancellationTokenRegistration? cancellationRegistration;
        lock (_lock)
        {
            cancellationRegistration = _cancellationRegistration;
        }

        cancellationRegistration?.Dispose();
    }

    private static void DisposeDeferred(GameInputCallbackRegistration registration)
    {
        ThreadPool.QueueUserWorkItem(static state => ((GameInputCallbackRegistration)state!).Dispose(), registration);
    }
}
