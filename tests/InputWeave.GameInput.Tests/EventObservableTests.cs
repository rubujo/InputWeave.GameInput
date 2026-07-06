namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class EventObservableTests
{
    [TestMethod]
    public void SubscribeInvokesOnFirstSubscribeExactlyOnce()
    {
        int startCount = 0;
        EventObservable<int> observable = new(() => startCount++, null);

        using IDisposable first = observable.Subscribe(new RecordingObserver());
        using IDisposable second = observable.Subscribe(new RecordingObserver());

        Assert.AreEqual(1, startCount);
    }

    [TestMethod]
    public void UnsubscribeAllInvokesOnLastUnsubscribeExactlyOnce()
    {
        int stopCount = 0;
        EventObservable<int> observable = new(null, () => stopCount++);

        IDisposable first = observable.Subscribe(new RecordingObserver());
        IDisposable second = observable.Subscribe(new RecordingObserver());

        first.Dispose();
        Assert.AreEqual(0, stopCount);

        second.Dispose();
        Assert.AreEqual(1, stopCount);
    }

    [TestMethod]
    public void OnNextDeliversValueToAllSubscribers()
    {
        EventObservable<int> observable = new();
        RecordingObserver first = new();
        RecordingObserver second = new();

        using IDisposable firstSubscription = observable.Subscribe(first);
        using IDisposable secondSubscription = observable.Subscribe(second);

        observable.OnNext(42);

        CollectionAssert.AreEqual(new[] { 42 }, first.Values);
        CollectionAssert.AreEqual(new[] { 42 }, second.Values);
    }

    [TestMethod]
    public void OnNextIsolatesThrowingObserverFromOthers()
    {
        EventObservable<int> observable = new();
        RecordingObserver throwing = new() { ThrowOnNext = true };
        RecordingObserver healthy = new();

        using IDisposable throwingSubscription = observable.Subscribe(throwing);
        using IDisposable healthySubscription = observable.Subscribe(healthy);

        observable.OnNext(1);

        CollectionAssert.AreEqual(new[] { 1 }, healthy.Values);
    }

    [TestMethod]
    public void DisposedSubscriptionDoesNotReceiveFurtherValues()
    {
        EventObservable<int> observable = new();
        RecordingObserver observer = new();

        IDisposable subscription = observable.Subscribe(observer);
        subscription.Dispose();
        observable.OnNext(1);

        Assert.IsEmpty(observer.Values);
    }

    [TestMethod]
    public void OnNextWithNoSubscribersDoesNotThrow()
    {
        EventObservable<int> observable = new();

        observable.OnNext(1);
    }

    [TestMethod]
    public void OnNextAfterAllSubscribersUnsubscribedDoesNotThrow()
    {
        EventObservable<int> observable = new();
        RecordingObserver observer = new();

        IDisposable subscription = observable.Subscribe(observer);
        subscription.Dispose();

        observable.OnNext(1);

        Assert.IsEmpty(observer.Values);
    }

    [TestMethod]
    public void CompleteNotifiesAllCurrentSubscribers()
    {
        EventObservable<int> observable = new();
        RecordingObserver first = new();
        RecordingObserver second = new();

        using IDisposable firstSubscription = observable.Subscribe(first);
        using IDisposable secondSubscription = observable.Subscribe(second);

        observable.Complete();

        Assert.AreEqual(1, first.CompletedCount);
        Assert.AreEqual(1, second.CompletedCount);
    }

    [TestMethod]
    public void CompleteIsolatesThrowingObserverFromOthers()
    {
        EventObservable<int> observable = new();
        RecordingObserver throwing = new() { ThrowOnCompleted = true };
        RecordingObserver healthy = new();

        using IDisposable throwingSubscription = observable.Subscribe(throwing);
        using IDisposable healthySubscription = observable.Subscribe(healthy);

        observable.Complete();

        Assert.AreEqual(1, healthy.CompletedCount);
    }

    [TestMethod]
    public void SubscribeAfterCompleteImmediatelyReceivesOnCompletedWithoutFurtherOnNext()
    {
        EventObservable<int> observable = new();
        observable.Complete();

        RecordingObserver observer = new();
        using IDisposable subscription = observable.Subscribe(observer);
        observable.OnNext(1);

        Assert.AreEqual(1, observer.CompletedCount);
        Assert.IsEmpty(observer.Values);
    }

    private sealed class RecordingObserver : IObserver<int>
    {
        public List<int> Values { get; } = [];

        public int CompletedCount { get; private set; }

        public bool ThrowOnNext { get; init; }

        public bool ThrowOnCompleted { get; init; }

        public void OnCompleted()
        {
            CompletedCount++;
            if (ThrowOnCompleted)
            {
                throw new InvalidOperationException("測試例外");
            }
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(int value)
        {
            if (ThrowOnNext)
            {
                throw new InvalidOperationException("測試例外");
            }

            Values.Add(value);
        }
    }
}
