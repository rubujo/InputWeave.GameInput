using InputWeave.GameInput;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputDeviceManagerTests
{
    [TestMethod]
    public void UseDevicesMatchesDevicesSnapshot()
    {
        RunWithManager(manager =>
        {
            manager.RefreshDevices();

            IReadOnlyList<GameInputDevice> expected = manager.Devices;
            IReadOnlyList<GameInputDevice>? actual = null;
            manager.UseDevices(devices => actual = devices);

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(expected.ToList(), actual!.ToList());
        });
    }

    [TestMethod]
    public void UseDeviceSnapshotsMatchesDeviceSnapshotsProperty()
    {
        RunWithManager(manager =>
        {
            manager.RefreshDevices();

            IReadOnlyList<GameInputDeviceInfoSnapshot> expected = manager.DeviceSnapshots;
            IReadOnlyList<GameInputDeviceInfoSnapshot>? actual = null;
            manager.UseDeviceSnapshots(snapshots => actual = snapshots);

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(expected.ToList(), actual!.ToList());
        });
    }

    [TestMethod]
    public void UseDevicesPropagatesCallbackException()
    {
        RunWithManager(manager =>
        {
            InvalidOperationException expected = new("測試例外");
            InvalidOperationException actual = Assert.ThrowsExactly<InvalidOperationException>(
                () => manager.UseDevices(_ => throw expected));
            Assert.AreSame(expected, actual);
        });
    }

    [TestMethod]
    public void UseDevicesThrowsAfterDispose()
    {
        RunWithManager(manager =>
        {
            manager.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => manager.UseDevices(static _ => { }));
        });
    }

    [TestMethod]
    public void UseDeviceSnapshotsThrowsAfterDispose()
    {
        RunWithManager(manager =>
        {
            manager.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => manager.UseDeviceSnapshots(static _ => { }));
        });
    }

    [TestMethod]
    public async Task RefreshDevicesAsyncMatchesRefreshDevices()
    {
        await RunWithManagerAsync(async manager =>
        {
            IReadOnlyList<GameInputDeviceInfoSnapshot> expected = manager.RefreshDevices();
            IReadOnlyList<GameInputDeviceInfoSnapshot> actual = await manager.RefreshDevicesAsync();

            Assert.AreEqual(expected.Count, actual.Count, "兩次列舉應回傳相同數量的裝置。");
            CollectionAssert.AreEquivalent(expected.ToList(), actual.ToList(), "兩次獨立原生列舉呼叫不保證裝置順序一致，只比較內容。");
        });
    }

    [TestMethod]
    public async Task WaitForDeviceEventAsyncCancelsWithoutNativeEvent()
    {
        await RunWithManagerAsync(async manager =>
        {
            using CancellationTokenSource cancellationSource = new();
            Task<GameInputDeviceManagerEvent> waitTask = manager.WaitForDeviceEventAsync(cancellationToken: cancellationSource.Token);
            cancellationSource.Cancel();

            await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => waitTask);
        });
    }

    [TestMethod]
    public async Task WaitForDeviceEventAsyncCompletesPromptlyWhenManagerDisposedWhilePending()
    {
        await RunWithManagerAsync(async manager =>
        {
            Task<GameInputDeviceManagerEvent> waitTask = manager.WaitForDeviceEventAsync();
            manager.Dispose();

            // 註冊裝置回呼時，GameInput 會先觸發一輪「目前已符合條件裝置」的初始回呼；
            // 如果這一輪剛好搶在 Dispose() 之前完成，waitTask 會用真實裝置資料正常回傳，
            // 而不是丟出 ObjectDisposedException——兩者都是正確行為，這裡只驗證「一定會儘快
            // 結束、不會永遠掛住」這個真正要修的不變量，不強求是哪一種完成方式。
            Task completedTask = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(2)));
            Assert.AreSame(waitTask, completedTask, "Dispose() 後等待應儘快完成，不應該永遠掛住。");

            if (waitTask.IsFaulted)
            {
                ObjectDisposedException exception = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => waitTask);
                Assert.AreEqual(nameof(GameInputDeviceManager), exception.ObjectName);
            }
        });
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

    private static async Task RunWithManagerAsync(Func<GameInputDeviceManager, Task> action)
    {
        try
        {
            using GameInputDeviceManager manager = GameInputDeviceManager.Create();
            await action(manager);
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
