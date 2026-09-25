namespace InputWeave.GameInput;

/// <summary>
/// A lightweight <see cref="IObservable{T}"/> implementation that does not depend on System.Reactive, used for internal data
/// pushing.
/// 輕量、不依賴 System.Reactive 的 <see cref="IObservable{T}"/> 實作，供內部推送資料使用。
/// </summary>
/// <typeparam name="T">The pushed data type. 推送資料型別。</typeparam>
internal sealed class EventObservable<T>(Action? onFirstSubscribe = null, Action? onLastUnsubscribe = null) : IObservable<T>
{
    // 以訂閱物件（而非 observer）為單位記錄，同一個 observer 訂閱多次時，取消其中一筆不會誤刪其他筆。
    private readonly List<Subscription> _subscriptions = [];
#if NET10_0_OR_GREATER
    private readonly System.Threading.Lock _lock = new();
    private readonly System.Threading.Lock _lifecycleLock = new();
#else
    private readonly object _lock = new();
    private readonly object _lifecycleLock = new();
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
        bool startNeeded = false;
        Subscription subscription = new(this, observer);
        lock (_lifecycleLock)
        {
            lock (_lock)
            {
                alreadyCompleted = _completed;
                if (!alreadyCompleted)
                {
                    bool wasEmpty = _subscriptions.Count == 0;
                    _subscriptions.Add(subscription);
                    startNeeded = wasEmpty;
                }
            }

            if (alreadyCompleted)
            {
                InvokeOnCompleted(observer);
            }
            else if (startNeeded)
            {
                try
                {
                    onFirstSubscribe?.Invoke();
                }
                catch
                {
                    lock (_lock)
                    {
                        _subscriptions.Remove(subscription);
                    }

                    throw;
                }
            }
        }

        return alreadyCompleted ? NoOpSubscription.Instance : subscription;
    }

    /// <summary>
    /// Takes a snapshot of the observers inside the lock and invokes them outside the lock, so a stuck observer cannot block the
    /// native callback thread while the lock is held, nor block other threads calling <see cref="Subscribe"/>,
    /// <see cref="OnNext"/>, or <see cref="Complete"/> on the same source. This sacrifices the strict serialization guarantee
    /// that no <see cref="OnNext"/> ever follows <see cref="Complete"/> (possible only in the extremely narrow window where
    /// <see cref="Complete"/> coincides with a native event), in exchange for avoiding the more serious risk of one stuck
    /// observer stalling the whole native callback thread.
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
            if (_completed || _subscriptions.Count == 0)
            {
                return;
            }

            snapshot = [.. _subscriptions.Select(static subscription => subscription.Observer)];
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
            snapshot = [.. _subscriptions.Select(static subscription => subscription.Observer)];
            _subscriptions.Clear();
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

    private void Unsubscribe(Subscription subscription)
    {
        bool stopNeeded = false;
        lock (_lifecycleLock)
        {
            lock (_lock)
            {
                bool removed = _subscriptions.Remove(subscription);
                if (removed && _subscriptions.Count == 0)
                {
                    stopNeeded = true;
                }
            }

            if (stopNeeded)
            {
                onLastUnsubscribe?.Invoke();
            }
        }
    }

    private sealed class Subscription(EventObservable<T> owner, IObserver<T> observer) : IDisposable
    {
        private int _disposed;

        public IObserver<T> Observer { get; } = observer;

        public void Dispose()
        {
            // 原子取得釋放權，並行 Dispose 只會取消這一筆訂閱一次。
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            {
                return;
            }

            owner.Unsubscribe(this);
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
