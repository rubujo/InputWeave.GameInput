using System.Reflection;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputDeviceManagerPushTests
{
    [TestMethod]
    public void DeviceChangedSubscribeAndUnsubscribeDoesNotThrow()
    {
        RunWithManager(manager =>
        {
            EventHandler<GameInputDeviceManagerEvent> handler = static (_, _) => { };

            manager.DeviceChanged += handler;
            manager.DeviceChanged -= handler;
        });
    }

    [TestMethod]
    public void DeviceChangesSubscribeAndDisposeDoesNotThrow()
    {
        RunWithManager(manager =>
        {
            IDisposable subscription = manager.DeviceChanges.Subscribe(new RecordingObserver());
            subscription.Dispose();
        });
    }

    [TestMethod]
    public void DeviceChangesSupportsMultipleConcurrentSubscribers()
    {
        RunWithManager(manager =>
        {
            RecordingObserver first = new();
            RecordingObserver second = new();

            using IDisposable firstSubscription = manager.DeviceChanges.Subscribe(first);
            using IDisposable secondSubscription = manager.DeviceChanges.Subscribe(second);
        });
    }

    [TestMethod]
    public void UnsubscribingDeviceChangedDoesNotStopManuallyStartedDeviceEvents()
    {
        RunWithManager(manager =>
        {
            manager.StartDeviceEvents();
            Assert.IsNotNull(GetDeviceEventsField(manager), "手動啟動後應有作用中的裝置事件註冊。");

            EventHandler<GameInputDeviceManagerEvent> handler = static (_, _) => { };
            manager.DeviceChanged += handler;
            manager.DeviceChanged -= handler;

            Assert.IsNotNull(GetDeviceEventsField(manager), "取消訂閱事件不應停止手動啟動的裝置事件監看。");

            manager.StopDeviceEvents();
        });
    }

    [TestMethod]
    public void NewSubscriberRestartsDeviceEventsAfterManualStop()
    {
        RunWithManager(manager =>
        {
            manager.StartDeviceEvents();
            manager.StopDeviceEvents();
            Assert.IsNull(GetDeviceEventsField(manager));

            using IDisposable subscription = manager.DeviceChanges.Subscribe(new RecordingObserver());

            Assert.IsNotNull(GetDeviceEventsField(manager), "手動停止後有新訂閱者加入時，應自動恢復裝置事件監看。");
        });
    }

    [TestMethod]
    public void ConcurrentManualAndPushOperationsDoNotThrowAndLeaveConsistentState()
    {
        RunWithManager(manager =>
        {
            const int iterations = 200;
            Exception? capturedException = null;

            Thread manualThread = new(() =>
            {
                try
                {
                    for (int index = 0; index < iterations; index++)
                    {
                        manager.StartDeviceEvents();
                        manager.StopDeviceEvents();
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref capturedException, ex, null);
                }
            });

            Thread pushThread = new(() =>
            {
                try
                {
                    for (int index = 0; index < iterations; index++)
                    {
                        EventHandler<GameInputDeviceManagerEvent> handler = static (_, _) => { };
                        manager.DeviceChanged += handler;
                        manager.DeviceChanged -= handler;
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref capturedException, ex, null);
                }
            });

            manualThread.Start();
            pushThread.Start();
            manualThread.Join();
            pushThread.Join();

            Assert.IsNull(capturedException);

            if (GetManualDeviceEventsActiveField(manager))
            {
                Assert.IsNotNull(GetDeviceEventsField(manager), "_manualDeviceEventsActive 為 true 時，_deviceEvents 不應為 null。");
            }

            manager.StopDeviceEvents();
        });
    }

    [TestMethod]
    public void UnsubscribingWhileExecutingCallbackDefersStopAndEventuallyStopsDeviceEvents()
    {
        RunWithManager(manager =>
        {
            EventHandler<GameInputDeviceManagerEvent> handler = static (_, _) => { };
            manager.DeviceChanged += handler;
            Assert.IsNotNull(GetDeviceEventsField(manager), "訂閱後應有作用中的裝置事件註冊。");

            GameInputCallbackThread.Enter();
            try
            {
                manager.DeviceChanged -= handler;
            }
            finally
            {
                GameInputCallbackThread.Exit();
            }

            Assert.IsTrue(
                SpinWait.SpinUntil(() => GetDeviceEventsField(manager) is null, TimeSpan.FromSeconds(2)),
                "在回呼執行緒中取消訂閱（模擬 handler 內自我取消訂閱）後，應延後到背景執行緒重新檢查並停止裝置事件監看，而不是讓原生註冊永遠不被釋放。");
        });
    }

    [TestMethod]
    public void StopDeviceEventsFromWithinNativeCallbackThreadDoesNotThrowAndClearsState()
    {
        RunWithManager(manager =>
        {
            manager.StartDeviceEvents();
            Assert.IsNotNull(GetDeviceEventsField(manager), "手動啟動後應有作用中的裝置事件註冊。");

            GameInputCallbackThread.Enter();
            try
            {
                manager.StopDeviceEvents();
            }
            finally
            {
                GameInputCallbackThread.Exit();
            }

            Assert.IsNull(GetDeviceEventsField(manager), "從原生回呼執行緒呼叫 StopDeviceEvents() 應透過 DisposeSafely() 同步完成，回傳時就應該已經清空。");
            Assert.IsFalse(GetManualDeviceEventsActiveField(manager), "StopDeviceEvents() 回傳後，_manualDeviceEventsActive 應正確重設為 false。");
        });
    }

    private static object? GetDeviceEventsField(GameInputDeviceManager manager)
    {
        FieldInfo field = typeof(GameInputDeviceManager).GetField("_deviceEvents", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return field.GetValue(manager);
    }

    private static bool GetManualDeviceEventsActiveField(GameInputDeviceManager manager)
    {
        FieldInfo field = typeof(GameInputDeviceManager).GetField("_manualDeviceEventsActive", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (bool)field.GetValue(manager)!;
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

    private sealed class RecordingObserver : IObserver<GameInputDeviceManagerEvent>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(GameInputDeviceManagerEvent value)
        {
        }
    }
}
