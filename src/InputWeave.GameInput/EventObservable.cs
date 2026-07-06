namespace InputWeave.GameInput;

/// <summary>
/// 輕量、不依賴 System.Reactive 的 <see cref="IObservable{T}"/> 實作，供內部推送資料使用。
/// </summary>
/// <typeparam name="T">推送資料型別。</typeparam>
internal sealed class EventObservable<T>(Action? onFirstSubscribe = null, Action? onLastUnsubscribe = null) : IObservable<T>
{
    private readonly List<IObserver<T>> _observers = [];
#if NET10_0_OR_GREATER
    private readonly System.Threading.Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private bool _completed;

    public IDisposable Subscribe(IObserver<T> observer)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(observer);
#else
        if (observer is null)
        {
            throw new ArgumentNullException(nameof(observer));
        }
#endif

        bool alreadyCompleted;
        lock (_lock)
        {
            alreadyCompleted = _completed;
            if (alreadyCompleted)
            {
                InvokeOnCompleted(observer);
            }
            else
            {
                bool wasEmpty = _observers.Count == 0;
                _observers.Add(observer);
                if (wasEmpty)
                {
                    try
                    {
                        onFirstSubscribe?.Invoke();
                    }
                    catch
                    {
                        _observers.Remove(observer);
                        throw;
                    }
                }
            }
        }

        return alreadyCompleted ? NoOpSubscription.Instance : new Subscription(this, observer);
    }

    /// <summary>
    /// 只在鎖定範圍內取快照，實際呼叫 observer 在鎖外執行，避免持有鎖期間卡住原生回呼執行緒、
    /// 或阻擋其他執行緒對同一個來源的 <see cref="Subscribe"/>／<see cref="OnNext"/>／<see cref="Complete"/> 呼叫。
    /// 這犧牲了「<see cref="Complete"/> 之後絕不再有 <see cref="OnNext"/>」的嚴格序列化保證
    /// （只有 <see cref="Complete"/> 剛好跟一筆原生事件同時發生的極窄時間窗才可能發生），
    /// 換取避免任何一個 observer 卡住就拖住整條原生回呼執行緒的更嚴重風險。
    /// </summary>
    public void OnNext(T value)
    {
        IObserver<T>[] snapshot;
        lock (_lock)
        {
            if (_completed || _observers.Count == 0)
            {
                return;
            }

            snapshot = [.. _observers];
        }

        foreach (IObserver<T> observer in snapshot)
        {
            InvokeOnNext(observer, value);
        }
    }

    public void Complete()
    {
        IObserver<T>[] snapshot;
        lock (_lock)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            snapshot = [.. _observers];
            _observers.Clear();
        }

        foreach (IObserver<T> observer in snapshot)
        {
            InvokeOnCompleted(observer);
        }
    }

    private static void InvokeOnNext(IObserver<T> observer, T value)
    {
        try
        {
            observer.OnNext(value);
        }
        catch (Exception ex)
        {
            GameInputClient.RaiseUnhandledCallbackException(ex);
        }
    }

    private static void InvokeOnCompleted(IObserver<T> observer)
    {
        try
        {
            observer.OnCompleted();
        }
        catch (Exception ex)
        {
            GameInputClient.RaiseUnhandledCallbackException(ex);
        }
    }

    private void Unsubscribe(IObserver<T> observer)
    {
        lock (_lock)
        {
            bool removed = _observers.Remove(observer);
            if (removed && _observers.Count == 0)
            {
                onLastUnsubscribe?.Invoke();
            }
        }
    }

    private sealed class Subscription(EventObservable<T> owner, IObserver<T> observer) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            owner.Unsubscribe(observer);
        }
    }

    private sealed class NoOpSubscription : IDisposable
    {
        public static readonly NoOpSubscription Instance = new();

        private NoOpSubscription()
        {
        }

        public void Dispose()
        {
        }
    }
}
