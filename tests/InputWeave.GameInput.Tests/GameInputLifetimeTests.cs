using System.Runtime.CompilerServices;
using InputWeave.GameInput.Interop;

using static InputWeave.GameInput.Tests.TestSupport;

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

            AssertReferenceCountSettles(observer.NativeInterface, before, $"列舉回呼包裝的裝置釋放後，原生參考計數應回到原值（before={before}）。");
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
            AssertReferenceCountSettles(observer.NativeInterface, before, $"裝置回呼結束後，原生參考計數應回到原值（before={before}）。");
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

            AssertReferenceCountSettles(observer.NativeInterface, before - 1, "並行 Dispose 只能釋放一次原生參考。");
        });
    }

    [TestMethod]
    public void UndisposedDeviceReleasesNativeReferenceWhenFinalized()
    {
        RunWithFirstDevice((client, observer) =>
        {
            uint before = GetReferenceCount(observer.NativeInterface);
            CreateUndisposedWrappers(client, observer.NativeInterface.Pointer);
            Assert.IsGreaterThan(before, GetReferenceCount(observer.NativeInterface), "未釋放的包裝應持有原生參考。");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            AssertReferenceCountSettles(observer.NativeInterface, before, "未呼叫 Dispose 的包裝被 GC 回收後，應由 SafeHandle 終結器釋放原生參考。");
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateUndisposedWrappers(GameInputClient client, IntPtr observedPointer)
    {
        // 在獨立的非內嵌方法中建立並丟棄包裝，確保呼叫端沒有殘留的區域參考讓物件保持存活。
        foreach (GameInputDevice device in client.EnumerateDevices(AnyCommonKind))
        {
            if (device.NativeInterface.Pointer != observedPointer)
            {
                device.Dispose();
            }
        }
    }

    /// <summary>
    /// 等待原生參考計數穩定到預期值。實機的計數是整個處理序共用的，其他測試在背景延後解除註冊時，
    /// 最後一次回呼可能短暫包裝同一個裝置；確定性的驗證由 GameInputFakeComLifetimeTests 負責。
    /// </summary>
    private static void AssertReferenceCountSettles(IGameInputDevice native, uint expected, string message)
    {
        bool settled = SpinWait.SpinUntil(() => GetReferenceCount(native) == expected, TimeSpan.FromSeconds(2));
        Assert.IsTrue(settled, $"{message}（預期 {expected}，實際 {GetReferenceCount(native)}）");
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

            // 先讓其他測試殘留、指向同一個原生裝置的包裝完成終結，避免它們在量測期間釋放參考而干擾計數。
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

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
    public void DisposingManagerFromDeviceChangedHandlerDoesNotDeadlock()
    {
        // 實機曾重現：在原生回呼中同步等待背景 UnregisterCallback 會互相等待而永久卡住。
        GameInputDeviceManager manager;
        try
        {
            manager = GameInputDeviceManager.Create();
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            Assert.Inconclusive($"此測試環境沒有可用的 GameInput 執行階段：{ex.Message}");
            return;
        }

        using ManualResetEventSlim handled = new();
        Exception? failure = null;
        int calls = 0;
        manager.DeviceChanged += (_, _) =>
        {
            if (Interlocked.Increment(ref calls) != 1)
            {
                return;
            }

            try
            {
                manager.Dispose();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                handled.Set();
            }
        };

        if (!SpinWait.SpinUntil(() => Volatile.Read(ref calls) > 0, TimeSpan.FromSeconds(5)))
        {
            manager.Dispose();
            Assert.Inconclusive("此測試需要至少一個已連線裝置，才能觸發初始裝置事件。");
        }

        Assert.IsTrue(handled.Wait(TimeSpan.FromSeconds(10)), "在 DeviceChanged 處理常式中 Dispose 管理器不得卡死。");
        Assert.IsNull(failure);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => manager.Devices);
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
}
