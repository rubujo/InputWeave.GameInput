using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputCallbackContextTests
{
    [TestMethod]
    public void DeactivateDropsHandler()
    {
        GameInputClient.DeviceCallbackContext context = new(static (_, _, _, _) => { });
        Assert.IsTrue(context.TryGetHandler(out _));

        context.Deactivate();

        Assert.IsFalse(context.IsActive);
        Assert.IsFalse(context.TryGetHandler(out GameInputDeviceHandler? handler));
        Assert.IsNull(handler);
    }

    [TestMethod]
    public void FailedUnregisterKeepsContextHandleButReleasesHandlerCapture()
    {
        (GameInputCallbackRegistration registration, GCHandle handle, WeakReference capture) = CreateRegistrationWithCapture(unregistered: false);

        registration.Dispose();
        ForceFullCollection();

        // UnregisterCallback 失敗時 GCHandle 必須保留，但處理常式捕捉的物件不應跟著永久存活。
        Assert.IsTrue(handle.IsAllocated);
        Assert.IsFalse(capture.IsAlive, "解除註冊失敗後，保留的回呼內容不應繼續持有處理常式捕捉的物件。");
        handle.Free();
    }

    [TestMethod]
    public void HeldRegistrationDoesNotKeepHandlerCaptureAfterDispose()
    {
        (GameInputCallbackRegistration registration, _, WeakReference capture) = CreateRegistrationWithCapture(unregistered: true);

        registration.Dispose();
        ForceFullCollection();

        // 使用者常把註冊存在欄位裡；註冊經由停用委派仍參考內容，釋放後處理常式捕捉的物件應可回收。
        Assert.IsTrue(registration.IsDisposed);
        Assert.IsFalse(capture.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (GameInputCallbackRegistration Registration, GCHandle Handle, WeakReference Capture) CreateRegistrationWithCapture(bool unregistered)
    {
        byte[] captured = new byte[1024];
        GameInputClient.ReadingCallbackContext context = new(_ => GC.KeepAlive(captured));
        GCHandle handle = GCHandle.Alloc(context);
        GameInputCallbackRegistration registration = new(
            token: 1,
            contextHandle: handle,
            deactivateContext: context.Deactivate,
            // 不可捕捉區域變數：C# 會讓同一範圍的 lambda 共用閉包類別，連帶讓 captured 一直存活。
            unregisterCallback: unregistered ? static _ => true : static _ => false,
            removeRegistration: static _ => { });
        return (registration, handle, new WeakReference(captured));
    }

    private static void ForceFullCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
