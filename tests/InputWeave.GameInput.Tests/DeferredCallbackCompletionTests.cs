using System.Runtime.InteropServices;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class DeferredCallbackCompletionTests
{
    [TestMethod]
    public void CompleteBeforeSetRegistrationStillDisposesRegistration()
    {
        DeferredCallbackCompletion completion = new();
        completion.Complete();

        GameInputCallbackRegistration registration = CreateRegistration();
        completion.SetRegistration(registration);

        Assert.IsTrue(SpinWait.SpinUntil(() => registration.IsDisposed, TimeSpan.FromSeconds(2)));
    }

    [TestMethod]
    public void SetRegistrationBeforeCompleteDisposesOnComplete()
    {
        DeferredCallbackCompletion completion = new();
        GameInputCallbackRegistration registration = CreateRegistration();
        completion.SetRegistration(registration);

        completion.Complete();

        Assert.IsTrue(SpinWait.SpinUntil(() => registration.IsDisposed, TimeSpan.FromSeconds(2)));
    }

    [TestMethod]
    public void CompleteIsIdempotent()
    {
        DeferredCallbackCompletion completion = new();
        GameInputCallbackRegistration registration = CreateRegistration();
        completion.SetRegistration(registration);

        completion.Complete();
        completion.Complete();

        Assert.IsTrue(SpinWait.SpinUntil(() => registration.IsDisposed, TimeSpan.FromSeconds(2)));
    }

    [TestMethod]
    public void CompleteBeforeSetCancellationRegistrationStillDisposesIt()
    {
        DeferredCallbackCompletion completion = new();
        completion.Complete();

        bool callbackInvoked = false;
        using CancellationTokenSource cancellationSource = new();
        CancellationTokenRegistration cancellationRegistration = cancellationSource.Token.Register(() => callbackInvoked = true);

        completion.SetCancellationRegistration(cancellationRegistration);
        cancellationSource.Cancel();

        Assert.IsFalse(callbackInvoked, "Complete() 已先發生，SetCancellationRegistration 應立即釋放註冊，取消時不應再觸發回呼。");
    }

    [TestMethod]
    public void DisposeCancellationRegistrationForDisposeDetachesCallbackWithoutCompleting()
    {
        DeferredCallbackCompletion completion = new();

        bool callbackInvoked = false;
        using CancellationTokenSource cancellationSource = new();
        CancellationTokenRegistration cancellationRegistration = cancellationSource.Token.Register(() => callbackInvoked = true);
        completion.SetCancellationRegistration(cancellationRegistration);

        completion.DisposeCancellationRegistrationForDispose();
        cancellationSource.Cancel();

        Assert.IsFalse(callbackInvoked, "DisposeCancellationRegistrationForDispose 後，取消 token 不應再觸發回呼。");

        GameInputCallbackRegistration registration = CreateRegistration();
        completion.SetRegistration(registration);
        completion.Complete();

        Assert.IsTrue(SpinWait.SpinUntil(() => registration.IsDisposed, TimeSpan.FromSeconds(2)), "DisposeCancellationRegistrationForDispose 不應影響後續正常的 Complete() 流程。");
    }

    private static GameInputCallbackRegistration CreateRegistration()
    {
        GCHandle handle = GCHandle.Alloc(new object());
        return new GameInputCallbackRegistration(
            token: 1,
            contextHandle: handle,
            deactivateContext: static () => { },
            stopCallback: static _ => { },
            unregisterCallback: static _ => true,
            removeRegistration: static _ => { });
    }
}
