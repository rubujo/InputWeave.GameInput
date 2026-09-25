using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput.Tests;

[TestClass]
[DoNotParallelize]
public sealed class GameInputLifetimeTests
{
    private const GameInputKind AnyCommonKind =
        GameInputKind.GameInputKindGamepad
        | GameInputKind.GameInputKindKeyboard
        | GameInputKind.GameInputKindMouse;

    [TestMethod]
    public void EnumerateDevicesDoesNotChangeNativeReferenceCount()
    {
        RunWithFirstDevice((client, observer) =>
        {
            uint before = GetReferenceCount(observer.NativeInterface);
            for (int round = 0; round < 2; round++)
            {
                foreach (GameInputDevice device in client.EnumerateDevices(AnyCommonKind))
                {
                    device.Dispose();
                }
            }

            Assert.AreEqual(before, GetReferenceCount(observer.NativeInterface), $"列舉回呼包裝的裝置釋放後，原生參考計數應回到原值（before={before}）。");
        });
    }

    [TestMethod]
    public void DeviceCallbackDoesNotChangeNativeReferenceCount()
    {
        RunWithFirstDevice((client, observer) =>
        {
            uint before = GetReferenceCount(observer.NativeInterface);
            int callbackCount = 0;
            using (client.RegisterDeviceCallback(
                null,
                AnyCommonKind,
                GameInputDeviceStatus.GameInputDeviceConnected,
                GameInputEnumerationKind.GameInputBlockingEnumeration,
                (_, _, _, _) => Interlocked.Increment(ref callbackCount)))
            {
            }

            Assert.IsGreaterThan(0, callbackCount, "阻塞式列舉應同步觸發至少一次裝置回呼。");
            Assert.AreEqual(before, GetReferenceCount(observer.NativeInterface), $"裝置回呼結束後，原生參考計數應回到原值（before={before}）。");
        });
    }

    [TestMethod]
    public void ConcurrentDeviceDisposeReleasesNativeReferenceOnce()
    {
        RunWithFirstDevice((client, observer) =>
        {
            IReadOnlyList<GameInputDevice> devices = client.EnumerateDevices(AnyCommonKind);
            GameInputDevice target = devices.First(device => device.NativeInterface.Pointer == observer.NativeInterface.Pointer);
            foreach (GameInputDevice other in devices.Where(device => !ReferenceEquals(device, target)))
            {
                other.Dispose();
            }

            uint before = GetReferenceCount(observer.NativeInterface);
            RunConcurrently(8, target.Dispose);

            Assert.AreEqual(before - 1, GetReferenceCount(observer.NativeInterface), "並行 Dispose 只能釋放一次原生參考。");
        });
    }

    private static uint GetReferenceCount(IGameInputDevice native)
    {
        uint count = native.AddRef();
        native.Release();
        return count - 1;
    }

    private static void RunWithFirstDevice(Action<GameInputClient, GameInputDevice> action)
    {
        RunWithClient(client =>
        {
            IReadOnlyList<GameInputDevice> devices = client.EnumerateDevices(AnyCommonKind);
            if (devices.Count == 0)
            {
                Assert.Inconclusive("此測試需要至少一個已連線的遊戲控制器、鍵盤或滑鼠。");
            }

            try
            {
                action(client, devices[0]);
            }
            finally
            {
                foreach (GameInputDevice device in devices)
                {
                    device.Dispose();
                }
            }
        });
    }

    [TestMethod]
    public void ClientCreatedOnStaThreadIsUsableFromMtaThreadsAndBack()
    {
        // .NET Framework 的 RCW 會綁定建立時的 COM apartment；互通層改用 vtable 函式指標後，
        // 在 STA 建立的物件必須能在 MTA 使用，MTA 取得的裝置也必須能回到 STA 使用。
        Exception? failure = null;
        bool inconclusive = false;
        Thread staThread = new(() =>
        {
            try
            {
                using GameInputClient client = GameInputClient.Create();
                GameInputDevice[] devices = [.. Task.Run(() => client.EnumerateDevicesAsync(AnyCommonKind)).GetAwaiter().GetResult()];
                try
                {
                    if (devices.Length == 0)
                    {
                        inconclusive = true;
                        return;
                    }

                    GameInputDeviceInfoSnapshot snapshot = devices[0].GetDeviceInfoSnapshot();
                    Assert.AreNotEqual(GameInputKind.GameInputKindUnknown, snapshot.SupportedInput, "STA 執行緒應能讀取 MTA 列舉到的裝置資訊。");
                    _ = Task.Run(client.GetCurrentTimestamp).GetAwaiter().GetResult();
                }
                finally
                {
                    foreach (GameInputDevice device in devices)
                    {
                        device.Dispose();
                    }
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                inconclusive = true;
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();

        if (failure is not null)
        {
            Assert.Fail($"跨 apartment 使用 GameInput 物件失敗：{failure}");
        }

        if (inconclusive)
        {
            Assert.Inconclusive("此測試需要 GameInput runtime 與至少一個已連線的遊戲控制器、鍵盤或滑鼠。");
        }
    }

    [TestMethod]
    public void ConcurrentClientDisposeDoesNotThrow()
    {
        RunWithClient(client =>
        {
            RunConcurrently(8, client.Dispose);
            Assert.ThrowsExactly<ObjectDisposedException>(() => client.GetCurrentTimestamp());
        });
    }

    [TestMethod]
    public async Task DisposeWhileEnumeratingKeepsNativeAliveUntilCallsReturn()
    {
        for (int iteration = 0; iteration < 20; iteration++)
        {
            await RunWithClientAsync(async client =>
            {
                Task<IReadOnlyList<GameInputDevice>>[] tasks = [.. Enumerable.Range(0, 8).Select(_ => Task.Run(() => client.EnumerateDevices(AnyCommonKind)))];
                client.Dispose();

                foreach (Task<IReadOnlyList<GameInputDevice>> task in tasks)
                {
                    try
                    {
                        foreach (GameInputDevice device in await task)
                        {
                            device.Dispose();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // 在 Dispose 之後才開始的列舉會拒絕執行；已在進行中的列舉則會正常完成。
                    }
                }
            });
        }
    }

    [TestMethod]
    public async Task DeviceManagerDisposeWhileRefreshingDoesNotLeakOrCrash()
    {
        for (int iteration = 0; iteration < 5; iteration++)
        {
            try
            {
                GameInputDeviceManager manager = GameInputDeviceManager.Create();
                Task[] tasks = [.. Enumerable.Range(0, 4).Select(_ => Task.Run(() => manager.RefreshDevices()))];
                RunConcurrently(4, manager.Dispose);

                foreach (Task task in tasks)
                {
                    try
                    {
                        await task;
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                Assert.ThrowsExactly<ObjectDisposedException>(() => manager.Devices);
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

    private static void RunConcurrently(int threadCount, Action action)
    {
        using Barrier barrier = new(threadCount);
        Task[] tasks = [.. Enumerable.Range(0, threadCount).Select(_ => Task.Factory.StartNew(
            () =>
            {
                barrier.SignalAndWait();
                action();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default))];
        Task.WaitAll(tasks);
    }

    private static void RunWithClient(Action<GameInputClient> action)
    {
        try
        {
            using GameInputClient client = GameInputClient.Create();
            action(client);
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

    private static async Task RunWithClientAsync(Func<GameInputClient, Task> action)
    {
        try
        {
            using GameInputClient client = GameInputClient.Create();
            await action(client);
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
