using System.Reflection;

namespace InputWeave.GameInput.Tests;

[TestClass]
[DoNotParallelize]
public sealed class GameInputEventReliabilityTests
{
    [TestMethod]
    public void RepeatedUnsubscribeDoesNotStopOtherSubscribers()
    {
        RunWithManager(manager =>
        {
            EventHandler<GameInputDeviceManagerEvent> keep = static (_, _) => { };
            EventHandler<GameInputDeviceManagerEvent> other = static (_, _) => { };
            EventHandler<GameInputDeviceManagerEvent> neverSubscribed = static (_, _) => { };
            manager.DeviceChanged += keep;
            manager.DeviceChanged += other;

            manager.DeviceChanged -= other;
            manager.DeviceChanged -= other;
            manager.DeviceChanged -= neverSubscribed;

            Assert.IsNotNull(GetDeviceEvents(manager), "重複取消訂閱或取消未訂閱的處理常式，不應停止其他訂閱者的裝置事件監看。");

            manager.DeviceChanged -= keep;
            Assert.IsNull(GetDeviceEvents(manager), "最後一個訂閱者取消訂閱後應停止裝置事件監看。");
        });
    }

    [TestMethod]
    public async Task WaitForDeviceEventIgnoresAlreadyConnectedDevices()
    {
        GameInputDeviceManager manager = CreateManagerWithDevices();
        using (manager)
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(1.5));
            Task<GameInputDeviceManagerEvent> wait = manager.WaitForDeviceEventAsync(timeout.Token);

            // 已連線的裝置不是「下一筆狀態變化」；測試期間沒有插拔時，工作應因逾時而取消。
            await Assert.ThrowsAsync<OperationCanceledException>(() => wait);
        }
    }

    [TestMethod]
    public void StopDeviceEventsDoesNotDeadlockWhenHandlerUnsubscribesDuringCallback()
    {
        GameInputDeviceManager manager = CreateManagerWithDevices();
        using ManualResetEventSlim handlerEntered = new();
        using ManualResetEventSlim stopStarted = new();
        EventHandler<GameInputDeviceManagerEvent>? handler = null;
        handler = (_, _) =>
        {
            if (handlerEntered.IsSet)
            {
                return;
            }

            handlerEntered.Set();

            // 等主執行緒開始停止監看（舊實作會在持有 _pushLock 時等待本回呼結束），再於回呼內取消訂閱。
            stopStarted.Wait(TimeSpan.FromSeconds(5));
            Thread.Sleep(200);
            manager.DeviceChanged -= handler;
        };

        manager.DeviceChanged += handler;
        if (!handlerEntered.Wait(TimeSpan.FromSeconds(5)))
        {
            manager.Dispose();
            Assert.Inconclusive("5 秒內沒有收到初始裝置事件，無法建立回呼進行中的情境。");
        }

        Task stop = Task.Run(manager.StopDeviceEvents);
        stopStarted.Set();

        Assert.IsTrue(stop.Wait(TimeSpan.FromSeconds(5)), "處理常式在回呼內取消訂閱時，StopDeviceEvents 不應死結。");
        Assert.IsNull(GetDeviceEvents(manager));
        manager.Dispose();
    }

    private static object? GetDeviceEvents(GameInputDeviceManager manager)
    {
        return typeof(GameInputDeviceManager)
            .GetField("_deviceEvents", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager);
    }

    private static GameInputDeviceManager CreateManagerWithDevices()
    {
        GameInputDeviceManager manager;
        try
        {
            manager = GameInputDeviceManager.Create();
        }
        catch (DllNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境未載入 GameInput.dll：{ex.Message}");
            throw;
        }
        catch (EntryPointNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境的 GameInput.dll 不含必要進入點：{ex.Message}");
            throw;
        }

        if (manager.RefreshDevices().Count == 0)
        {
            manager.Dispose();
            Assert.Inconclusive("此測試需要至少一個已連線的裝置。");
        }

        return manager;
    }

    private static void RunWithManager(Action<GameInputDeviceManager> action)
    {
        try
        {
            using GameInputDeviceManager manager = GameInputDeviceManager.Create();
            action(manager);
        }
        catch (DllNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境未載入 GameInput.dll：{ex.Message}");
        }
        catch (EntryPointNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境的 GameInput.dll 不含必要進入點：{ex.Message}");
        }
    }
}
