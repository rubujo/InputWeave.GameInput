using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using InputWeave.GameInput.Interop;
using static InputWeave.GameInput.Tests.TestSupport;

namespace InputWeave.GameInput.Tests;

/// <summary>
/// 以記憶體中的假 COM 物件驗證包裝的參考計數與生命週期，不需要 GameInput 執行階段或實體裝置，
/// 讓 CI 也能攔下過度釋放、重複釋放、洩漏與呼叫途中被釋放等回歸。
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class GameInputFakeComLifetimeTests
{
    [TestMethod]
    public void DeviceDisposeReleasesOwnedReferenceExactlyOnce()
    {
        using FakeComObject fake = FakeComObject.CreateDevice();
        GameInputDevice device = new(new IGameInputDevice(fake.Pointer));

        device.Dispose();
        device.Dispose();

        Assert.AreEqual(0, fake.RefCount, "重複 Dispose 只能釋放一次擁有的參考。");
    }

    [TestMethod]
    public void ConcurrentDeviceDisposeReleasesOwnedReferenceExactlyOnce()
    {
        for (int iteration = 0; iteration < 50; iteration++)
        {
            using FakeComObject fake = FakeComObject.CreateDevice();
            GameInputDevice device = new(new IGameInputDevice(fake.Pointer));

            RunConcurrently(8, device.Dispose);

            Assert.AreEqual(0, fake.RefCount, "並行 Dispose 只能釋放一次擁有的參考。");
        }
    }

    [TestMethod]
    public void UndisposedDeviceIsReleasedByFinalizer()
    {
        using FakeComObject fake = FakeComObject.CreateDevice();
        CreateAndDropDevice(fake.Pointer);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.AreEqual(0, fake.RefCount, "未呼叫 Dispose 的包裝被 GC 回收後，應由 SafeHandle 終結器釋放參考。");
    }

    [TestMethod]
    public void DeviceDisposeDuringNativeCallDefersReleaseUntilCallReturns()
    {
        using FakeComObject fake = FakeComObject.CreateDevice();
        GameInputDevice device = new(new IGameInputDevice(fake.Pointer));
        int refCountDuringCall = -1;
        FakeComObject.OnNativeCall = () =>
        {
            device.Dispose();
            refCountDuringCall = fake.RefCount;
        };

        try
        {
            _ = device.IsConnected();
        }
        finally
        {
            FakeComObject.OnNativeCall = null;
        }

        Assert.AreEqual(1, refCountDuringCall, "原生呼叫進行中被 Dispose 時，租約應讓原生物件存活到呼叫返回。");
        Assert.AreEqual(0, fake.RefCount, "呼叫返回後才應釋放參考。");
    }

    [TestMethod]
    public void DeviceCallAfterDisposeThrowsWithoutTouchingNativeObject()
    {
        using FakeComObject fake = FakeComObject.CreateDevice();
        GameInputDevice device = new(new IGameInputDevice(fake.Pointer));
        device.Dispose();
        int callsBefore = fake.NativeCallCount;

        ObjectDisposedException exception = Assert.ThrowsExactly<ObjectDisposedException>(() => device.IsConnected());

        Assert.AreEqual(nameof(GameInputDevice), exception.ObjectName);
        Assert.AreEqual(callsBefore, fake.NativeCallCount, "Dispose 後不得再呼叫原生物件。");
    }

    [TestMethod]
    public void WrapBorrowedDeviceTakesItsOwnReference()
    {
        // 原生回呼傳入的指標由呼叫端擁有（初始參考計數 1），包裝必須另外 AddRef。
        using FakeComObject fake = FakeComObject.CreateDevice();
        GameInputDevice device = GameInputClient.WrapBorrowedDevice(new IGameInputDevice(fake.Pointer));

        Assert.AreEqual(2, fake.RefCount, "包裝借用指標時應取得自己的參考。");
        device.Dispose();
        Assert.AreEqual(1, fake.RefCount, "包裝釋放後不得動到呼叫端擁有的參考。");
    }

    [TestMethod]
    public void WrapBorrowedReadingTakesItsOwnReference()
    {
        using FakeComObject fake = FakeComObject.CreateReading();
        GameInputReading reading = GameInputClient.WrapBorrowedReading(new IGameInputReading(fake.Pointer));

        Assert.AreEqual(2, fake.RefCount, "包裝借用指標時應取得自己的參考。");
        Assert.AreEqual(FakeComObject.Timestamp, reading.Timestamp);
        reading.Dispose();
        Assert.AreEqual(1, fake.RefCount, "包裝釋放後不得動到呼叫端擁有的參考。");
    }

    [TestMethod]
    public void ConcurrentClientDisposeReleasesRootExactlyOnce()
    {
        for (int iteration = 0; iteration < 50; iteration++)
        {
            using FakeComObject fake = FakeComObject.CreateGameInput();
            GameInputClient client = new(new GameInputComHandle(fake.Pointer));
            Assert.AreEqual(FakeComObject.Timestamp, client.GetCurrentTimestamp());

            RunConcurrently(8, client.Dispose);

            Assert.AreEqual(0, fake.RefCount, "並行 Dispose 只能釋放一次根物件參考。");
            ObjectDisposedException exception = Assert.ThrowsExactly<ObjectDisposedException>(() => client.GetCurrentTimestamp());
            Assert.AreEqual(nameof(GameInputClient), exception.ObjectName);
        }
    }

    [TestMethod]
    public void ClientDisposeDuringNativeCallDefersReleaseUntilCallReturns()
    {
        using FakeComObject fake = FakeComObject.CreateGameInput();
        GameInputClient client = new(new GameInputComHandle(fake.Pointer));
        int refCountDuringCall = -1;
        FakeComObject.OnNativeCall = () =>
        {
            client.Dispose();
            refCountDuringCall = fake.RefCount;
        };

        ulong timestamp;
        try
        {
            timestamp = client.GetCurrentTimestamp();
        }
        finally
        {
            FakeComObject.OnNativeCall = null;
        }

        Assert.AreEqual(FakeComObject.Timestamp, timestamp);
        Assert.AreEqual(1, refCountDuringCall, "原生呼叫進行中被 Dispose 時，租約應讓根物件存活到呼叫返回。");
        Assert.AreEqual(0, fake.RefCount, "呼叫返回後才應釋放根物件參考。");
    }

    [TestMethod]
    public void UndisposedClientIsReleasedByFinalizer()
    {
        using FakeComObject fake = FakeComObject.CreateGameInput();
        CreateAndDropClient(fake.Pointer);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.AreEqual(0, fake.RefCount, "未呼叫 Dispose 的用戶端被 GC 回收後，應由 SafeHandle 終結器釋放根物件參考。");
    }

    [TestMethod]
    public async Task AwaitableCallbackFaultsTaskWhenConversionThrows()
    {
        using FakeComObject fake = FakeComObject.CreateGameInput();
        using GameInputClient client = new(new GameInputComHandle(fake.Pointer));
        InvalidOperationException conversionFailure = new("轉換失敗");
        GameInputCallbackRegistration? registration = null;

        Task<int> task = client.RunAwaitableCallback<int>(
            (_, onError) =>
            {
                registration = new GameInputCallbackRegistration(
                    token: 1,
                    GCHandle.Alloc(new object()),
                    deactivateContext: static () => { },
                    unregisterCallback: static _ => true,
                    removeRegistration: static _ => { });
                ThreadPool.QueueUserWorkItem(_ => onError(conversionFailure));
                return registration;
            },
            CancellationToken.None,
            nameof(GameInputClient));

        Task completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.AreSame(task, completed, "回呼內轉換失敗時，等待中的工作應以例外結束，而不是永遠不會完成。");
        InvalidOperationException actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => task);
        Assert.AreSame(conversionFailure, actual);
        Assert.IsTrue(SpinWait.SpinUntil(() => registration!.IsDisposed, TimeSpan.FromSeconds(2)), "工作失敗後應解除一次性回呼註冊。");
    }

    [TestMethod]
    public void DeviceEventQueueIsBoundedAndDropsOldestEvents()
    {
        using FakeComObject fake = FakeComObject.CreateGameInput();
        using GameInputDeviceManager manager = new(new GameInputClient(new GameInputComHandle(fake.Pointer)));
        int total = GameInputDeviceManager.MaxQueuedEvents + 500;

        for (int index = 0; index < total; index++)
        {
            manager.PublishDeviceEvent(new GameInputDeviceManagerEvent(
                (ulong)index,
                GameInputDeviceEvent.GameInputDeviceEventConnected,
                default));
        }

        List<ulong> queued = [];
        while (manager.TryDequeueEvent(out GameInputDeviceManagerEvent managerEvent))
        {
            queued.Add(managerEvent.Timestamp);
        }

        Assert.HasCount(GameInputDeviceManager.MaxQueuedEvents, queued, "佇列應只保留上限數量的事件。");
        Assert.AreEqual((ulong)(total - GameInputDeviceManager.MaxQueuedEvents), queued[0], "滿了應丟棄最舊的事件。");
        Assert.AreEqual((ulong)(total - 1), queued[queued.Count - 1]);
    }

    [TestMethod]
    public void DeviceArgumentStaysAliveWhenDisposedDuringClientCall()
    {
        using FakeComObject fakeRoot = FakeComObject.CreateGameInput();
        using FakeComObject fakeDevice = FakeComObject.CreateDevice();
        using GameInputClient client = new(new GameInputComHandle(fakeRoot.Pointer));
        GameInputDevice device = new(new IGameInputDevice(fakeDevice.Pointer));
        int refCountDuringCall = -1;
        FakeComObject.OnNativeCall = () =>
        {
            device.Dispose();
            refCountDuringCall = fakeDevice.RefCount;
        };

        try
        {
            Assert.IsNull(client.GetCurrentReading(GameInputKind.GameInputKindGamepad, device));
        }
        finally
        {
            FakeComObject.OnNativeCall = null;
        }

        Assert.AreEqual(1, refCountDuringCall, "當成參數傳入的裝置在原生呼叫期間被 Dispose 時，租約應讓它存活到呼叫返回。");
        Assert.AreEqual(0, fakeDevice.RefCount);
    }

    [TestMethod]
    public void EnumerateDevicesDisposesCollectedDevicesWhenRegistrationFails()
    {
        using FakeComObject fakeRoot = FakeComObject.CreateGameInput();
        using FakeComObject fakeDevice = FakeComObject.CreateDevice();
        using GameInputClient client = new(new GameInputComHandle(fakeRoot.Pointer));
        FakeComObject.EnumeratedDevice = fakeDevice.Pointer;

        try
        {
            _ = Assert.ThrowsExactly<GameInputException>(() => client.EnumerateDevices(GameInputKind.GameInputKindGamepad));
        }
        finally
        {
            FakeComObject.EnumeratedDevice = IntPtr.Zero;
        }

        Assert.AreEqual(1, fakeDevice.RefCount, "列舉失敗時，回呼已收集的裝置包裝應立即釋放自己的參考。");
    }

    [TestMethod]
    public void DisposeSafelyOnCallbackThreadDoesNotWaitForUnregister()
    {
        // 模擬 UnregisterCallback 要等目前回呼返回才會完成；舊實作在回呼執行緒上同步等待，會與它互相等待而死結。
        using ManualResetEventSlim callbackReturned = new();
        bool deactivated = false;
        int ownerLeaseAcquired = 0;
        int ownerLeaseReleased = 0;
        GameInputCallbackRegistration registration = new(
            token: 1,
            GCHandle.Alloc(new object()),
            deactivateContext: () => deactivated = true,
            unregisterCallback: _ =>
            {
                callbackReturned.Wait();
                return true;
            },
            removeRegistration: static _ => { },
            acquireOwnerLease: () =>
            {
                Interlocked.Increment(ref ownerLeaseAcquired);
                return () => Interlocked.Increment(ref ownerLeaseReleased);
            });

        Exception? failure = null;
        Thread callbackThread = new(() =>
        {
            GameInputCallbackThread.Enter();
            try
            {
                failure = registration.DisposeSafely();
            }
            finally
            {
                GameInputCallbackThread.Exit();
            }
        });
        callbackThread.Start();
        bool returned = callbackThread.Join(TimeSpan.FromSeconds(5));
        callbackReturned.Set();

        Assert.IsTrue(returned, "回呼執行緒上的 DisposeSafely 不得等待背景解除註冊，否則會與 UnregisterCallback 死結。");
        Assert.IsNull(failure);
        Assert.IsTrue(deactivated, "應立即停用處理常式。");
        Assert.AreEqual(1, Volatile.Read(ref ownerLeaseAcquired), "應在回呼執行緒上先取得擁有者租約。");
        Assert.IsTrue(
            SpinWait.SpinUntil(() => registration.IsDisposed && Volatile.Read(ref ownerLeaseReleased) == 1, TimeSpan.FromSeconds(5)),
            "背景解除註冊完成後應歸還擁有者租約。");
    }
    [TestMethod]
    public void DeviceGettersRunUnderOneLeaseEvenWhenDisposed()
    {
        // 原生呼叫期間 Dispose 裝置，並在參考歸零時把顯示名稱記憶體改寫，模擬原生物件已被回收。
        // 若讀取指標發生在租約結束之後，會讀到改寫後的內容。
        using FakeComObject fake = FakeComObject.CreateDevice("Fake Pad");
        GameInputDevice device = new(new IGameInputDevice(fake.Pointer));
        FakeComObject.OnNativeCall = device.Dispose;
        FakeComObject.OnFinalRelease = fake.PoisonDisplayName;

        GameInputDeviceInfoSnapshot snapshot;
        try
        {
            snapshot = device.GetDeviceInfoSnapshot();
        }
        finally
        {
            FakeComObject.OnNativeCall = null;
            FakeComObject.OnFinalRelease = null;
        }

        Assert.AreEqual("Fake Pad", snapshot.DisplayName, "API 4 getter 必須在同一個租約內完成，避免中途 Dispose 提早釋放。");
        Assert.AreEqual(0, fake.RefCount);
    }

    [TestMethod]
    public void OversizedDeviceStringThrowsBeforeAllocation()
    {
        using FakeComObject fake = FakeComObject.CreateDevice("Fake Pad");
        using GameInputDevice device = new(new IGameInputDevice(fake.Pointer));
        FakeComObject.ReturnOversizedString = true;

        try
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => device.GetDeviceInfoSnapshot());
        }
        finally
        {
            FakeComObject.ReturnOversizedString = false;
        }
    }

    [TestMethod]
    public void RawDataCopyAndSetUseTheRequestedArraySegment()
    {
        using FakeComObject fake = FakeComObject.CreateRawReport();
        using GameInputRawDeviceReport report = new(new IGameInputRawDeviceReport(fake.Pointer));

        byte[] buffer = new byte[6];
        Assert.AreEqual(3, report.CopyRawData(buffer, 2, 3));
        CollectionAssert.AreEqual(new byte[] { 0, 0, 1, 2, 3, 0 }, buffer, "原生端應直接寫入指定的陣列區段。");
        Assert.AreEqual(0, report.CopyRawData([]), "空緩衝區也應傳入有效指標並正常返回。");

        Assert.IsTrue(report.SetRawData([9, 8, 7, 6], 1, 2));
        CollectionAssert.AreEqual(new byte[] { 8, 7 }, FakeComObject.LastSetRawData, "原生端應讀到指定的陣列區段。");
        Assert.IsTrue(report.SetRawData([]));
        CollectionAssert.AreEqual(Array.Empty<byte>(), FakeComObject.LastSetRawData);
    }

    [TestMethod]
    public void MapperReadsMappingThroughStackBuffer()
    {
        using FakeComObject fake = FakeComObject.CreateMapper();
        using GameInputMapper mapper = new(new IGameInputMapper(fake.Pointer));

        Assert.IsTrue(mapper.TryGetGamepadAxisMappingInfo((GameInputGamepadAxes)1, out GameInputAxisMapping mapping));

        Assert.AreEqual((GameInputElementKind)2, mapping.ControllerElementKind);
        Assert.AreEqual(7u, mapping.ControllerIndex);
        Assert.IsTrue(mapping.IsInverted);
        Assert.IsFalse(mapping.FromTwoButtons);
        Assert.AreEqual(3u, mapping.ButtonMinIndexValue);
        Assert.AreEqual(GameInputSwitchPosition.GameInputSwitchUp, mapping.ReferenceDirection);
    }

    [TestMethod]
    public void CreateForceFeedbackEffectPassesParametersThroughStackBuffer()
    {
        using FakeComObject fake = FakeComObject.CreateDevice();
        using GameInputDevice device = new(new IGameInputDevice(fake.Pointer));
        GameInputForceFeedbackParams parameters = new() { Kind = (GameInputForceFeedbackEffectKind)3 };

        _ = Assert.ThrowsExactly<GameInputException>(() => device.CreateForceFeedbackEffect(0, in parameters));

        Assert.AreEqual((GameInputForceFeedbackEffectKind)3, FakeComObject.LastEffectKind);
    }

    [TestMethod]
    public void ConcurrentRumbleScopeDisposeClearsOnce()
    {
        for (int iteration = 0; iteration < 50; iteration++)
        {
            int clears = 0;
            GameInputRumbleScope scope = new(() => Interlocked.Increment(ref clears));

            RunConcurrently(8, scope.Dispose);

            Assert.AreEqual(1, clears, "並行 Dispose 只能清除一次震動狀態。");
            Assert.IsTrue(scope.IsDisposed);
        }
    }
    [TestMethod]
    public void TryGetCurrentGamepadReturnsSnapshotWithoutUnwrappingNullable()
    {
        using FakeComObject fakeRoot = FakeComObject.CreateGameInput();
        using FakeComObject fakeReading = FakeComObject.CreateReading();
        using GameInputDeviceManager manager = new(new GameInputClient(new GameInputComHandle(fakeRoot.Pointer)));
        FakeComObject.CurrentReading = fakeReading.Pointer;

        bool found;
        GamepadReadingSnapshot snapshot;
        try
        {
            found = manager.TryGetCurrentGamepad(out snapshot);
        }
        finally
        {
            FakeComObject.CurrentReading = IntPtr.Zero;
        }

        Assert.IsTrue(found);
        Assert.AreEqual(FakeComObject.Timestamp, snapshot.Timestamp);
        Assert.IsTrue(snapshot.IsButtonDown(GameInputGamepadButtons.GameInputGamepadA));
        Assert.AreEqual(0.5f, snapshot.State.LeftTrigger);
        Assert.AreEqual(1, fakeReading.RefCount, "短命 reading 讀完後應釋放 out 參數交出的參考。");
    }

    [TestMethod]
    public void TryGetCurrentGamepadReturnsFalseWhenNoReadingIsAvailable()
    {
        using FakeComObject fakeRoot = FakeComObject.CreateGameInput();
        using GameInputDeviceManager manager = new(new GameInputClient(new GameInputComHandle(fakeRoot.Pointer)));

        Assert.IsFalse(manager.TryGetCurrentGamepad(out GamepadReadingSnapshot snapshot));
        Assert.AreEqual(default, snapshot);
        Assert.IsFalse(manager.TryGetCurrentKeyboard(out _));
    }

    [TestMethod]
    public void TryGetCurrentThrowsAfterManagerDispose()
    {
        using FakeComObject fakeRoot = FakeComObject.CreateGameInput();
        GameInputDeviceManager manager = new(new GameInputClient(new GameInputComHandle(fakeRoot.Pointer)));
        manager.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => manager.TryGetCurrentGamepad(out _));
    }
    [TestMethod]
    public void RegistrationFreesContextOnlyAfterSuccessfulUnregister()
    {
        (GameInputCallbackRegistration registration, WeakReference context, Func<bool> deactivated) = CreateRegistration(unregister: _ => true);

        registration.Dispose();
        CollectGarbage();

        Assert.IsTrue(deactivated(), "釋放註冊時應先停用 context。");
        Assert.IsFalse(context.IsAlive, "UnregisterCallback 成功後應釋放 context 的 GCHandle。");
    }

    [TestMethod]
    public void RegistrationKeepsContextWhenUnregisterFails()
    {
        (GameInputCallbackRegistration registration, WeakReference context, Func<bool> deactivated) = CreateRegistration(unregister: _ => false);

        registration.Dispose();
        CollectGarbage();

        Assert.IsTrue(deactivated(), "即使解除註冊失敗，也應停用 context，讓仍在進行的回呼不再執行處理常式。");
        Assert.IsTrue(context.IsAlive, "UnregisterCallback 失敗時不得釋放 GCHandle，避免原生端仍在進行的回呼存取已釋放的 handle。");
    }

    [TestMethod]
    public void RegistrationKeepsContextWhenUnregisterThrows()
    {
        (GameInputCallbackRegistration registration, WeakReference context, Func<bool> deactivated) =
            CreateRegistration(unregister: _ => throw new ObjectDisposedException(nameof(GameInputClient)));

        _ = Assert.ThrowsExactly<ObjectDisposedException>(registration.Dispose);
        CollectGarbage();

        Assert.IsTrue(registration.IsDisposed);
        Assert.IsTrue(deactivated());
        Assert.IsTrue(context.IsAlive, "UnregisterCallback 拋出例外時不得釋放 GCHandle。");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (GameInputCallbackRegistration Registration, WeakReference Context, Func<bool> Deactivated) CreateRegistration(Func<ulong, bool> unregister)
    {
        // 在獨立的非內嵌方法中建立 context，確保只有 GCHandle 讓它保持存活。
        object context = new();
        bool deactivated = false;
        GameInputCallbackRegistration registration = new(
            token: 1,
            GCHandle.Alloc(context),
            deactivateContext: () => deactivated = true,
            unregisterCallback: unregister,
            removeRegistration: static _ => { });
        return (registration, new WeakReference(context), () => deactivated);
    }

    private static void CollectGarbage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateAndDropDevice(IntPtr pointer)
    {
        _ = new GameInputDevice(new IGameInputDevice(pointer));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateAndDropClient(IntPtr pointer)
    {
        _ = new GameInputClient(new GameInputComHandle(pointer));
    }

    /// <summary>
    /// 記憶體中的假 COM 物件，配置為 [vtable 指標][參考計數][原生呼叫次數]。
    /// 以 <see cref="Marshal.GetFunctionPointerForDelegate{TDelegate}(TDelegate)"/> 產生 vtable 函式指標，
    /// 三個目標框架都適用；委派存放在靜態欄位，確保原生端呼叫期間不會被 GC 回收。
    /// </summary>
    private sealed unsafe class FakeComObject : IDisposable
    {
        public const ulong Timestamp = 42;

        private static readonly RefCountFunction s_addRef = AddRef;
        private static readonly RefCountFunction s_release = Release;
        private static readonly TimestampFunction s_timestamp = GetTimestamp;
        private static readonly GetCurrentReadingFunction s_getCurrentReading = GetCurrentReading;
        private static readonly RegisterDeviceCallbackFunction s_registerDeviceCallback = RegisterDeviceCallback;
        private static readonly CreateForceFeedbackEffectFunction s_createForceFeedbackEffect = CreateForceFeedbackEffect;
        private static readonly GetRawDataFunction s_getRawData = GetRawData;
        private static readonly SetRawDataFunction s_setRawData = SetRawData;
        private static readonly GetAxisMappingFunction s_getAxisMapping = GetGamepadAxisMappingInfo;
        private static readonly GetGamepadStateFunction s_getGamepadState = GetGamepadState;

        private readonly IntPtr _vtbl;
        private IntPtr _displayName;
        private int _displayNameLength;
        private bool _disposed;

        private FakeComObject(IntPtr vtbl)
        {
            _vtbl = vtbl;
            Pointer = Marshal.AllocHGlobal((2 * IntPtr.Size) + (2 * sizeof(int)));
            *(IntPtr*)Pointer = vtbl;
            *RefCountSlot(Pointer) = 1;
            *CallCountSlot(Pointer) = 0;
            *DisplayNameSlot(Pointer) = IntPtr.Zero;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint RefCountFunction(IntPtr self);


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte GetGamepadStateFunction(IntPtr self, IntPtr state);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateForceFeedbackEffectFunction(IntPtr self, uint motorIndex, IntPtr parameters, IntPtr effect);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate UIntPtr GetRawDataFunction(IntPtr self, UIntPtr bufferSize, IntPtr buffer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte SetRawDataFunction(IntPtr self, UIntPtr bufferSize, IntPtr buffer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte GetAxisMappingFunction(IntPtr self, GameInputGamepadAxes axisElement, IntPtr mapping);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate ulong TimestampFunction(IntPtr self);


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetCurrentReadingFunction(IntPtr self, GameInputKind inputKind, IntPtr device, IntPtr reading);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int RegisterDeviceCallbackFunction(IntPtr self, IntPtr device, GameInputKind inputKind, IntPtr context, IntPtr callbackFunc, IntPtr callbackToken);

        public static Action? OnNativeCall { get; set; }

        /// <summary>
        /// 參考計數歸零時觸發，用來模擬原生物件釋放後記憶體被回收。
        /// </summary>
        public static Action? OnFinalRelease { get; set; }

        /// <summary>
        /// 為 true 時，API 4 字串 getter 回報超過上限的長度。
        /// </summary>
        public static bool ReturnOversizedString { get; set; }

        public static byte[]? LastSetRawData { get; set; }

        /// <summary>
        /// GetCurrentReading 要回傳的 reading；為 IntPtr.Zero 時回報找不到 reading。
        /// </summary>
        public static IntPtr CurrentReading { get; set; }

        public static GameInputForceFeedbackEffectKind? LastEffectKind { get; set; }

        /// <summary>
        /// RegisterDeviceCallback 在回報失敗前，先以此裝置同步觸發一次回呼；為 IntPtr.Zero 時不觸發。
        /// </summary>
        public static IntPtr EnumeratedDevice { get; set; }

        public IntPtr Pointer { get; }

        public int RefCount
        {
            get
            {
                return Volatile.Read(ref *RefCountSlot(Pointer));
            }
        }

        public int NativeCallCount
        {
            get
            {
                return Volatile.Read(ref *CallCountSlot(Pointer));
            }
        }

        public static FakeComObject CreateDevice(string? displayName = null)
        {
            IntPtr vtbl = AllocateVtbl(sizeof(IGameInputDeviceVtbl));
            IGameInputDeviceVtbl* table = (IGameInputDeviceVtbl*)vtbl;
            table->AddRef = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_addRef);
            table->Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_release);
            table->CreateForceFeedbackEffect = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, void**, int>)Marshal.GetFunctionPointerForDelegate(s_createForceFeedbackEffect);
            table->GetVendorId = (delegate* unmanaged[Stdcall]<IntPtr, ushort>)Marshal.GetFunctionPointerForDelegate(s_api4GetVendorId);
            table->GetProductId = (delegate* unmanaged[Stdcall]<IntPtr, ushort>)Marshal.GetFunctionPointerForDelegate(s_api4GetProductId);
            table->GetRevisionNumber = (delegate* unmanaged[Stdcall]<IntPtr, ushort>)Marshal.GetFunctionPointerForDelegate(s_api4GetRevisionNumber);
            table->GetUsage = (delegate* unmanaged[Stdcall]<IntPtr, GameInputUsage*, void>)Marshal.GetFunctionPointerForDelegate(s_api4GetUsage);
            table->GetHardwareVersion = (delegate* unmanaged[Stdcall]<IntPtr, GameInputVersion*, void>)Marshal.GetFunctionPointerForDelegate(s_api4GetHardwareVersion);
            table->GetFirmwareVersion = (delegate* unmanaged[Stdcall]<IntPtr, GameInputVersion*, void>)Marshal.GetFunctionPointerForDelegate(s_api4GetFirmwareVersion);
            table->GetDeviceId = (delegate* unmanaged[Stdcall]<IntPtr, AppLocalDeviceId*, void>)Marshal.GetFunctionPointerForDelegate(s_api4GetDeviceId);
            table->GetDeviceRootId = (delegate* unmanaged[Stdcall]<IntPtr, AppLocalDeviceId*, void>)Marshal.GetFunctionPointerForDelegate(s_api4GetDeviceRootId);
            table->GetDeviceFamily = (delegate* unmanaged[Stdcall]<IntPtr, GameInputDeviceFamily>)Marshal.GetFunctionPointerForDelegate(s_api4GetDeviceFamily);
            table->GetSupportedInput = (delegate* unmanaged[Stdcall]<IntPtr, GameInputKind>)Marshal.GetFunctionPointerForDelegate(s_api4GetSupportedInput);
            table->GetSupportedRumbleMotors = (delegate* unmanaged[Stdcall]<IntPtr, GameInputRumbleMotors>)Marshal.GetFunctionPointerForDelegate(s_api4GetSupportedRumbleMotors);
            table->GetSupportedSystemButtons = (delegate* unmanaged[Stdcall]<IntPtr, GameInputSystemButtons>)Marshal.GetFunctionPointerForDelegate(s_api4GetSupportedSystemButtons);
            table->GetSupportedLightingGroups = (delegate* unmanaged[Stdcall]<IntPtr, GameInputLightingGroups>)Marshal.GetFunctionPointerForDelegate(s_api4GetSupportedLightingGroups);
            table->GetSupportedLightingGroupSize = (delegate* unmanaged[Stdcall]<IntPtr, GameInputLightingGroups, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetSupportedLightingGroupSize);
            table->GetContainerId = (delegate* unmanaged[Stdcall]<IntPtr, Guid*, void>)Marshal.GetFunctionPointerForDelegate(s_api4GetContainerId);
            table->GetDisplayName = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetDisplayName);
            table->GetPnpPath = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetPnpPath);
            table->GetXInputDeviceIndex = (delegate* unmanaged[Stdcall]<IntPtr, uint*, byte>)Marshal.GetFunctionPointerForDelegate(s_api4GetXInputDeviceIndex);
            table->GetKeyboardInfo = (delegate* unmanaged[Stdcall]<IntPtr, GameInputKeyboardInfo*, byte>)Marshal.GetFunctionPointerForDelegate(s_api4GetKeyboardInfo);
            table->GetMouseInfo = (delegate* unmanaged[Stdcall]<IntPtr, GameInputMouseInfo*, byte>)Marshal.GetFunctionPointerForDelegate(s_api4GetMouseInfo);
            table->GetSensorsInfo = (delegate* unmanaged[Stdcall]<IntPtr, GameInputSensorsInfo*, byte>)Marshal.GetFunctionPointerForDelegate(s_api4GetSensorsInfo);
            table->GetControllerAxisCount = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetControllerAxisCount);
            table->GetControllerAxisLabels = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetControllerAxisLabels);
            table->GetControllerButtonCount = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetControllerButtonCount);
            table->GetControllerButtonLabels = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetControllerButtonLabels);
            table->GetControllerSwitchCount = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetControllerSwitchCount);
            table->GetControllerSwitchInfo = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetControllerSwitchInfo);
            table->GetArcadeStickInfo = (delegate* unmanaged[Stdcall]<IntPtr, GameInputArcadeStickInfo*, byte>)Marshal.GetFunctionPointerForDelegate(s_api4GetArcadeStickInfo);
            table->GetFlightStickInfo = (delegate* unmanaged[Stdcall]<IntPtr, GameInputFlightStickInfo*, byte>)Marshal.GetFunctionPointerForDelegate(s_api4GetFlightStickInfo);
            table->GetGamepadInfo = (delegate* unmanaged[Stdcall]<IntPtr, GameInputGamepadInfo*, byte>)Marshal.GetFunctionPointerForDelegate(s_api4GetGamepadInfo);
            table->GetRacingWheelInfo = (delegate* unmanaged[Stdcall]<IntPtr, GameInputRacingWheelInfo*, byte>)Marshal.GetFunctionPointerForDelegate(s_api4GetRacingWheelInfo);
            table->GetForceFeedbackMotorCount = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetForceFeedbackMotorCount);
            table->GetForceFeedbackMotorInfo = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetForceFeedbackMotorInfo);
            table->GetInputReportCount = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetInputReportCount);
            table->GetInputReportInfo = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetInputReportInfo);
            table->GetOutputReportCount = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetOutputReportCount);
            table->GetOutputReportInfo = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetOutputReportInfo);
            table->GetFeatureReportCount = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetFeatureReportCount);
            table->GetFeatureReportInfo = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetFeatureReportInfo);
            table->GetHapticInfo = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)Marshal.GetFunctionPointerForDelegate(s_api4GetHapticInfo);
            table->IsConnected = (delegate* unmanaged[Stdcall]<IntPtr, byte>)Marshal.GetFunctionPointerForDelegate(s_api4IsConnected);
            table->IsForceFeedbackMotorPoweredOn = (delegate* unmanaged[Stdcall]<IntPtr, uint, byte>)Marshal.GetFunctionPointerForDelegate(s_api4IsForceFeedbackMotorPoweredOn);
            table->SetForceFeedbackMotorGain = (delegate* unmanaged[Stdcall]<IntPtr, uint, float, void>)Marshal.GetFunctionPointerForDelegate(s_api4SetForceFeedbackMotorGain);
            table->SetRumbleState = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, void>)Marshal.GetFunctionPointerForDelegate(s_api4SetRumbleState);
            table->DirectInputEscape = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, uint, IntPtr, uint, uint*, int>)Marshal.GetFunctionPointerForDelegate(s_api4DirectInputEscape);
            table->CreateInputMapper = (delegate* unmanaged[Stdcall]<IntPtr, void**, int>)Marshal.GetFunctionPointerForDelegate(s_api4CreateInputMapper);
            table->GetExtraAxisCount = (delegate* unmanaged[Stdcall]<IntPtr, GameInputKind, uint*, int>)Marshal.GetFunctionPointerForDelegate(s_api4GetExtraAxisCount);
            table->GetExtraButtonCount = (delegate* unmanaged[Stdcall]<IntPtr, GameInputKind, uint*, int>)Marshal.GetFunctionPointerForDelegate(s_api4GetExtraButtonCount);
            table->GetExtraAxisIndexes = (delegate* unmanaged[Stdcall]<IntPtr, GameInputKind, uint, IntPtr, int>)Marshal.GetFunctionPointerForDelegate(s_api4GetExtraAxisIndexes);
            table->GetExtraButtonIndexes = (delegate* unmanaged[Stdcall]<IntPtr, GameInputKind, uint, IntPtr, int>)Marshal.GetFunctionPointerForDelegate(s_api4GetExtraButtonIndexes);
            table->CreateRawDeviceReport = (delegate* unmanaged[Stdcall]<IntPtr, uint, GameInputRawDeviceReportKind, void**, int>)Marshal.GetFunctionPointerForDelegate(s_api4CreateRawDeviceReport);
            table->GetRawDeviceFeature = (delegate* unmanaged[Stdcall]<IntPtr, uint, void**, int>)Marshal.GetFunctionPointerForDelegate(s_api4GetRawDeviceFeature);
            table->SetRawDeviceFeature = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)Marshal.GetFunctionPointerForDelegate(s_api4SetRawDeviceFeature);
            table->SendRawDeviceOutput = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)Marshal.GetFunctionPointerForDelegate(s_api4SendRawDeviceOutput);
            table->SetLightingColor = (delegate* unmanaged[Stdcall]<IntPtr, GameInputColor, GameInputLightingGroups, uint, void>)Marshal.GetFunctionPointerForDelegate(s_api4SetLightingColor);
            table->ApplyLightingColors = (delegate* unmanaged[Stdcall]<IntPtr, void>)Marshal.GetFunctionPointerForDelegate(s_api4ApplyLightingColors);
            table->ApplyLightingSystemDefault = (delegate* unmanaged[Stdcall]<IntPtr, void>)Marshal.GetFunctionPointerForDelegate(s_api4ApplyLightingSystemDefault);
            table->GetTouchSurfaceCount = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_api4GetTouchSurfaceCount);
            table->GetTouchSurfaceInfo = (delegate* unmanaged[Stdcall]<IntPtr, uint, GameInputTouchSurfaceInfo*, int>)Marshal.GetFunctionPointerForDelegate(s_api4GetTouchSurfaceInfo);
            FakeComObject fake = new(vtbl);
            if (displayName is not null)
            {
                byte[] utf8 = Encoding.UTF8.GetBytes(displayName + '\0');
                fake._displayName = Marshal.AllocHGlobal(utf8.Length);
                fake._displayNameLength = utf8.Length - 1;
                Marshal.Copy(utf8, 0, fake._displayName, utf8.Length);
                *DisplayNameSlot(fake.Pointer) = fake._displayName;
            }

            return fake;
        }

        public static FakeComObject CreateRawReport()
        {
            IntPtr vtbl = AllocateVtbl(sizeof(IGameInputRawDeviceReportVtbl));
            IGameInputRawDeviceReportVtbl* table = (IGameInputRawDeviceReportVtbl*)vtbl;
            table->AddRef = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_addRef);
            table->Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_release);
            table->GetRawData = (delegate* unmanaged[Stdcall]<IntPtr, UIntPtr, IntPtr, UIntPtr>)Marshal.GetFunctionPointerForDelegate(s_getRawData);
            table->SetRawData = (delegate* unmanaged[Stdcall]<IntPtr, UIntPtr, IntPtr, byte>)Marshal.GetFunctionPointerForDelegate(s_setRawData);
            return new FakeComObject(vtbl);
        }

        public static FakeComObject CreateMapper()
        {
            IntPtr vtbl = AllocateVtbl(sizeof(IGameInputMapperVtbl));
            IGameInputMapperVtbl* table = (IGameInputMapperVtbl*)vtbl;
            table->AddRef = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_addRef);
            table->Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_release);
            table->GetGamepadAxisMappingInfo = (delegate* unmanaged[Stdcall]<IntPtr, GameInputGamepadAxes, IntPtr, byte>)Marshal.GetFunctionPointerForDelegate(s_getAxisMapping);
            return new FakeComObject(vtbl);
        }

        public void PoisonDisplayName()
        {
            for (int index = 0; index < _displayNameLength; index++)
            {
                Marshal.WriteByte(_displayName, index, (byte)'X');
            }
        }

        public static FakeComObject CreateReading()
        {
            IntPtr vtbl = AllocateVtbl(sizeof(IGameInputReadingVtbl));
            IGameInputReadingVtbl* table = (IGameInputReadingVtbl*)vtbl;
            table->AddRef = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_addRef);
            table->Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_release);
            table->GetTimestamp = (delegate* unmanaged[Stdcall]<IntPtr, ulong>)Marshal.GetFunctionPointerForDelegate(s_timestamp);
            table->GetGamepadState = (delegate* unmanaged[Stdcall]<IntPtr, GameInputGamepadState*, byte>)Marshal.GetFunctionPointerForDelegate(s_getGamepadState);
            return new FakeComObject(vtbl);
        }

        public static FakeComObject CreateGameInput()
        {
            IntPtr vtbl = AllocateVtbl(sizeof(IGameInputVtbl));
            IGameInputVtbl* table = (IGameInputVtbl*)vtbl;
            table->AddRef = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_addRef);
            table->Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_release);
            table->GetCurrentTimestamp = (delegate* unmanaged[Stdcall]<IntPtr, ulong>)Marshal.GetFunctionPointerForDelegate(s_timestamp);
            table->GetCurrentReading = (delegate* unmanaged[Stdcall]<IntPtr, GameInputKind, IntPtr, void**, int>)Marshal.GetFunctionPointerForDelegate(s_getCurrentReading);
            table->RegisterDeviceCallback = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, GameInputKind, IntPtr, IntPtr, ulong*, int>)Marshal.GetFunctionPointerForDelegate(s_registerDeviceCallback);
            return new FakeComObject(vtbl);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Marshal.FreeHGlobal(Pointer);
            Marshal.FreeHGlobal(_vtbl);
            if (_displayName != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_displayName);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate ushort Api4GetVendorIdFunction(IntPtr p0);
        private static readonly Api4GetVendorIdFunction s_api4GetVendorId = Api4GetVendorId;
        private static ushort Api4GetVendorId(IntPtr p0)
        {
            OnNativeCall?.Invoke();
            return 0;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate ushort Api4GetProductIdFunction(IntPtr p0);
        private static readonly Api4GetProductIdFunction s_api4GetProductId = Api4GetProductId;
        private static ushort Api4GetProductId(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate ushort Api4GetRevisionNumberFunction(IntPtr p0);
        private static readonly Api4GetRevisionNumberFunction s_api4GetRevisionNumber = Api4GetRevisionNumber;
        private static ushort Api4GetRevisionNumber(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void Api4GetUsageFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetUsageFunction s_api4GetUsage = Api4GetUsage;
        private static void Api4GetUsage(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void Api4GetHardwareVersionFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetHardwareVersionFunction s_api4GetHardwareVersion = Api4GetHardwareVersion;
        private static void Api4GetHardwareVersion(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void Api4GetFirmwareVersionFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetFirmwareVersionFunction s_api4GetFirmwareVersion = Api4GetFirmwareVersion;
        private static void Api4GetFirmwareVersion(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void Api4GetDeviceIdFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetDeviceIdFunction s_api4GetDeviceId = Api4GetDeviceId;
        private static void Api4GetDeviceId(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void Api4GetDeviceRootIdFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetDeviceRootIdFunction s_api4GetDeviceRootId = Api4GetDeviceRootId;
        private static void Api4GetDeviceRootId(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate GameInputDeviceFamily Api4GetDeviceFamilyFunction(IntPtr p0);
        private static readonly Api4GetDeviceFamilyFunction s_api4GetDeviceFamily = Api4GetDeviceFamily;
        private static GameInputDeviceFamily Api4GetDeviceFamily(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate GameInputKind Api4GetSupportedInputFunction(IntPtr p0);
        private static readonly Api4GetSupportedInputFunction s_api4GetSupportedInput = Api4GetSupportedInput;
        private static GameInputKind Api4GetSupportedInput(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate GameInputRumbleMotors Api4GetSupportedRumbleMotorsFunction(IntPtr p0);
        private static readonly Api4GetSupportedRumbleMotorsFunction s_api4GetSupportedRumbleMotors = Api4GetSupportedRumbleMotors;
        private static GameInputRumbleMotors Api4GetSupportedRumbleMotors(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate GameInputSystemButtons Api4GetSupportedSystemButtonsFunction(IntPtr p0);
        private static readonly Api4GetSupportedSystemButtonsFunction s_api4GetSupportedSystemButtons = Api4GetSupportedSystemButtons;
        private static GameInputSystemButtons Api4GetSupportedSystemButtons(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate GameInputLightingGroups Api4GetSupportedLightingGroupsFunction(IntPtr p0);
        private static readonly Api4GetSupportedLightingGroupsFunction s_api4GetSupportedLightingGroups = Api4GetSupportedLightingGroups;
        private static GameInputLightingGroups Api4GetSupportedLightingGroups(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetSupportedLightingGroupSizeFunction(IntPtr p0, GameInputLightingGroups p1);
        private static readonly Api4GetSupportedLightingGroupSizeFunction s_api4GetSupportedLightingGroupSize = Api4GetSupportedLightingGroupSize;
        private static uint Api4GetSupportedLightingGroupSize(IntPtr p0, GameInputLightingGroups p1)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void Api4GetContainerIdFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetContainerIdFunction s_api4GetContainerId = Api4GetContainerId;
        private static void Api4GetContainerId(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetDisplayNameFunction(IntPtr p0, uint p1, IntPtr p2);
        private static readonly Api4GetDisplayNameFunction s_api4GetDisplayName = Api4GetDisplayName;
        private static uint Api4GetDisplayName(IntPtr p0, uint p1, IntPtr p2)
        {
            if (ReturnOversizedString)
            {
                return uint.MaxValue;
            }
            IntPtr info = *DisplayNameSlot(p0);
            if (info == IntPtr.Zero)
            {
                return 0;
            }
            IntPtr text = info;
            int length = 0;
            while (Marshal.ReadByte(text, length) != 0)
            {
                length++;
            }
            byte[] bytes = new byte[length + 1];
            Marshal.Copy(text, bytes, 0, bytes.Length);
            if (p1 >= bytes.Length && p2 != IntPtr.Zero)
            {
                Marshal.Copy(bytes, 0, p2, bytes.Length);
            }
            return (uint)bytes.Length;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetPnpPathFunction(IntPtr p0, uint p1, IntPtr p2);
        private static readonly Api4GetPnpPathFunction s_api4GetPnpPath = Api4GetPnpPath;
        private static uint Api4GetPnpPath(IntPtr p0, uint p1, IntPtr p2)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte Api4GetXInputDeviceIndexFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetXInputDeviceIndexFunction s_api4GetXInputDeviceIndex = Api4GetXInputDeviceIndex;
        private static byte Api4GetXInputDeviceIndex(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte Api4GetKeyboardInfoFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetKeyboardInfoFunction s_api4GetKeyboardInfo = Api4GetKeyboardInfo;
        private static byte Api4GetKeyboardInfo(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte Api4GetMouseInfoFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetMouseInfoFunction s_api4GetMouseInfo = Api4GetMouseInfo;
        private static byte Api4GetMouseInfo(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte Api4GetSensorsInfoFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetSensorsInfoFunction s_api4GetSensorsInfo = Api4GetSensorsInfo;
        private static byte Api4GetSensorsInfo(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetControllerAxisCountFunction(IntPtr p0);
        private static readonly Api4GetControllerAxisCountFunction s_api4GetControllerAxisCount = Api4GetControllerAxisCount;
        private static uint Api4GetControllerAxisCount(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetControllerAxisLabelsFunction(IntPtr p0, uint p1, IntPtr p2);
        private static readonly Api4GetControllerAxisLabelsFunction s_api4GetControllerAxisLabels = Api4GetControllerAxisLabels;
        private static uint Api4GetControllerAxisLabels(IntPtr p0, uint p1, IntPtr p2)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetControllerButtonCountFunction(IntPtr p0);
        private static readonly Api4GetControllerButtonCountFunction s_api4GetControllerButtonCount = Api4GetControllerButtonCount;
        private static uint Api4GetControllerButtonCount(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetControllerButtonLabelsFunction(IntPtr p0, uint p1, IntPtr p2);
        private static readonly Api4GetControllerButtonLabelsFunction s_api4GetControllerButtonLabels = Api4GetControllerButtonLabels;
        private static uint Api4GetControllerButtonLabels(IntPtr p0, uint p1, IntPtr p2)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetControllerSwitchCountFunction(IntPtr p0);
        private static readonly Api4GetControllerSwitchCountFunction s_api4GetControllerSwitchCount = Api4GetControllerSwitchCount;
        private static uint Api4GetControllerSwitchCount(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetControllerSwitchInfoFunction(IntPtr p0, uint p1, IntPtr p2);
        private static readonly Api4GetControllerSwitchInfoFunction s_api4GetControllerSwitchInfo = Api4GetControllerSwitchInfo;
        private static uint Api4GetControllerSwitchInfo(IntPtr p0, uint p1, IntPtr p2)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte Api4GetArcadeStickInfoFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetArcadeStickInfoFunction s_api4GetArcadeStickInfo = Api4GetArcadeStickInfo;
        private static byte Api4GetArcadeStickInfo(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte Api4GetFlightStickInfoFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetFlightStickInfoFunction s_api4GetFlightStickInfo = Api4GetFlightStickInfo;
        private static byte Api4GetFlightStickInfo(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte Api4GetGamepadInfoFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetGamepadInfoFunction s_api4GetGamepadInfo = Api4GetGamepadInfo;
        private static byte Api4GetGamepadInfo(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte Api4GetRacingWheelInfoFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetRacingWheelInfoFunction s_api4GetRacingWheelInfo = Api4GetRacingWheelInfo;
        private static byte Api4GetRacingWheelInfo(IntPtr p0, IntPtr p1)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetForceFeedbackMotorCountFunction(IntPtr p0);
        private static readonly Api4GetForceFeedbackMotorCountFunction s_api4GetForceFeedbackMotorCount = Api4GetForceFeedbackMotorCount;
        private static uint Api4GetForceFeedbackMotorCount(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetForceFeedbackMotorInfoFunction(IntPtr p0, uint p1, IntPtr p2);
        private static readonly Api4GetForceFeedbackMotorInfoFunction s_api4GetForceFeedbackMotorInfo = Api4GetForceFeedbackMotorInfo;
        private static uint Api4GetForceFeedbackMotorInfo(IntPtr p0, uint p1, IntPtr p2)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetInputReportCountFunction(IntPtr p0);
        private static readonly Api4GetInputReportCountFunction s_api4GetInputReportCount = Api4GetInputReportCount;
        private static uint Api4GetInputReportCount(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetInputReportInfoFunction(IntPtr p0, uint p1, IntPtr p2);
        private static readonly Api4GetInputReportInfoFunction s_api4GetInputReportInfo = Api4GetInputReportInfo;
        private static uint Api4GetInputReportInfo(IntPtr p0, uint p1, IntPtr p2)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetOutputReportCountFunction(IntPtr p0);
        private static readonly Api4GetOutputReportCountFunction s_api4GetOutputReportCount = Api4GetOutputReportCount;
        private static uint Api4GetOutputReportCount(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetOutputReportInfoFunction(IntPtr p0, uint p1, IntPtr p2);
        private static readonly Api4GetOutputReportInfoFunction s_api4GetOutputReportInfo = Api4GetOutputReportInfo;
        private static uint Api4GetOutputReportInfo(IntPtr p0, uint p1, IntPtr p2)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetFeatureReportCountFunction(IntPtr p0);
        private static readonly Api4GetFeatureReportCountFunction s_api4GetFeatureReportCount = Api4GetFeatureReportCount;
        private static uint Api4GetFeatureReportCount(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetFeatureReportInfoFunction(IntPtr p0, uint p1, IntPtr p2);
        private static readonly Api4GetFeatureReportInfoFunction s_api4GetFeatureReportInfo = Api4GetFeatureReportInfo;
        private static uint Api4GetFeatureReportInfo(IntPtr p0, uint p1, IntPtr p2)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4GetHapticInfoFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4GetHapticInfoFunction s_api4GetHapticInfo = Api4GetHapticInfo;
        private static int Api4GetHapticInfo(IntPtr p0, IntPtr p1)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte Api4IsConnectedFunction(IntPtr p0);
        private static readonly Api4IsConnectedFunction s_api4IsConnected = Api4IsConnected;
        private static byte Api4IsConnected(IntPtr p0)
        {
            Interlocked.Increment(ref *CallCountSlot(p0));
            OnNativeCall?.Invoke();
            return 1;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate byte Api4IsForceFeedbackMotorPoweredOnFunction(IntPtr p0, uint p1);
        private static readonly Api4IsForceFeedbackMotorPoweredOnFunction s_api4IsForceFeedbackMotorPoweredOn = Api4IsForceFeedbackMotorPoweredOn;
        private static byte Api4IsForceFeedbackMotorPoweredOn(IntPtr p0, uint p1)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void Api4SetForceFeedbackMotorGainFunction(IntPtr p0, uint p1, float p2);
        private static readonly Api4SetForceFeedbackMotorGainFunction s_api4SetForceFeedbackMotorGain = Api4SetForceFeedbackMotorGain;
        private static void Api4SetForceFeedbackMotorGain(IntPtr p0, uint p1, float p2)
        {
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void Api4SetRumbleStateFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4SetRumbleStateFunction s_api4SetRumbleState = Api4SetRumbleState;
        private static void Api4SetRumbleState(IntPtr p0, IntPtr p1)
        {
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4DirectInputEscapeFunction(IntPtr p0, uint p1, IntPtr p2, uint p3, IntPtr p4, uint p5, IntPtr p6);
        private static readonly Api4DirectInputEscapeFunction s_api4DirectInputEscape = Api4DirectInputEscape;
        private static int Api4DirectInputEscape(IntPtr p0, uint p1, IntPtr p2, uint p3, IntPtr p4, uint p5, IntPtr p6)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4CreateInputMapperFunction(IntPtr p0, void** p1);
        private static readonly Api4CreateInputMapperFunction s_api4CreateInputMapper = Api4CreateInputMapper;
        private static int Api4CreateInputMapper(IntPtr p0, void** p1)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4GetExtraAxisCountFunction(IntPtr p0, GameInputKind p1, IntPtr p2);
        private static readonly Api4GetExtraAxisCountFunction s_api4GetExtraAxisCount = Api4GetExtraAxisCount;
        private static int Api4GetExtraAxisCount(IntPtr p0, GameInputKind p1, IntPtr p2)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4GetExtraButtonCountFunction(IntPtr p0, GameInputKind p1, IntPtr p2);
        private static readonly Api4GetExtraButtonCountFunction s_api4GetExtraButtonCount = Api4GetExtraButtonCount;
        private static int Api4GetExtraButtonCount(IntPtr p0, GameInputKind p1, IntPtr p2)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4GetExtraAxisIndexesFunction(IntPtr p0, GameInputKind p1, uint p2, IntPtr p3);
        private static readonly Api4GetExtraAxisIndexesFunction s_api4GetExtraAxisIndexes = Api4GetExtraAxisIndexes;
        private static int Api4GetExtraAxisIndexes(IntPtr p0, GameInputKind p1, uint p2, IntPtr p3)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4GetExtraButtonIndexesFunction(IntPtr p0, GameInputKind p1, uint p2, IntPtr p3);
        private static readonly Api4GetExtraButtonIndexesFunction s_api4GetExtraButtonIndexes = Api4GetExtraButtonIndexes;
        private static int Api4GetExtraButtonIndexes(IntPtr p0, GameInputKind p1, uint p2, IntPtr p3)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4CreateRawDeviceReportFunction(IntPtr p0, uint p1, GameInputRawDeviceReportKind p2, void** p3);
        private static readonly Api4CreateRawDeviceReportFunction s_api4CreateRawDeviceReport = Api4CreateRawDeviceReport;
        private static int Api4CreateRawDeviceReport(IntPtr p0, uint p1, GameInputRawDeviceReportKind p2, void** p3)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4GetRawDeviceFeatureFunction(IntPtr p0, uint p1, void** p2);
        private static readonly Api4GetRawDeviceFeatureFunction s_api4GetRawDeviceFeature = Api4GetRawDeviceFeature;
        private static int Api4GetRawDeviceFeature(IntPtr p0, uint p1, void** p2)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4SetRawDeviceFeatureFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4SetRawDeviceFeatureFunction s_api4SetRawDeviceFeature = Api4SetRawDeviceFeature;
        private static int Api4SetRawDeviceFeature(IntPtr p0, IntPtr p1)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4SendRawDeviceOutputFunction(IntPtr p0, IntPtr p1);
        private static readonly Api4SendRawDeviceOutputFunction s_api4SendRawDeviceOutput = Api4SendRawDeviceOutput;
        private static int Api4SendRawDeviceOutput(IntPtr p0, IntPtr p1)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void Api4SetLightingColorFunction(IntPtr p0, GameInputColor p1, GameInputLightingGroups p2, uint p3);
        private static readonly Api4SetLightingColorFunction s_api4SetLightingColor = Api4SetLightingColor;
        private static void Api4SetLightingColor(IntPtr p0, GameInputColor p1, GameInputLightingGroups p2, uint p3)
        {
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void Api4ApplyLightingColorsFunction(IntPtr p0);
        private static readonly Api4ApplyLightingColorsFunction s_api4ApplyLightingColors = Api4ApplyLightingColors;
        private static void Api4ApplyLightingColors(IntPtr p0)
        {
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void Api4ApplyLightingSystemDefaultFunction(IntPtr p0);
        private static readonly Api4ApplyLightingSystemDefaultFunction s_api4ApplyLightingSystemDefault = Api4ApplyLightingSystemDefault;
        private static void Api4ApplyLightingSystemDefault(IntPtr p0)
        {
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint Api4GetTouchSurfaceCountFunction(IntPtr p0);
        private static readonly Api4GetTouchSurfaceCountFunction s_api4GetTouchSurfaceCount = Api4GetTouchSurfaceCount;
        private static uint Api4GetTouchSurfaceCount(IntPtr p0)
        {
            return default;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Api4GetTouchSurfaceInfoFunction(IntPtr p0, uint p1, IntPtr p2);
        private static readonly Api4GetTouchSurfaceInfoFunction s_api4GetTouchSurfaceInfo = Api4GetTouchSurfaceInfo;
        private static int Api4GetTouchSurfaceInfo(IntPtr p0, uint p1, IntPtr p2)
        {
            // 輸出結構由呼叫端初始化為 default；假端不需解參考。
            return default;
        }

        private static IntPtr AllocateVtbl(int size)
        {
            IntPtr vtbl = Marshal.AllocHGlobal(size);
            byte* bytes = (byte*)vtbl;
            for (int index = 0; index < size; index++)
            {
                bytes[index] = 0;
            }

            return vtbl;
        }

        private static int* RefCountSlot(IntPtr self)
        {
            return (int*)((byte*)self + IntPtr.Size);
        }

        private static IntPtr* DisplayNameSlot(IntPtr self)
        {
            return (IntPtr*)((byte*)self + IntPtr.Size + (2 * sizeof(int)));
        }

        private static int* CallCountSlot(IntPtr self)
        {
            return (int*)((byte*)self + IntPtr.Size + sizeof(int));
        }

        private static uint AddRef(IntPtr self)
        {
            return (uint)Interlocked.Increment(ref *RefCountSlot(self));
        }

        private static uint Release(IntPtr self)
        {
            int count = Interlocked.Decrement(ref *RefCountSlot(self));
            if (count == 0)
            {
                OnFinalRelease?.Invoke();
            }

            return (uint)count;
        }

        private static ulong GetTimestamp(IntPtr self)
        {
            Interlocked.Increment(ref *CallCountSlot(self));
            OnNativeCall?.Invoke();
            return Timestamp;
        }

        private static int GetCurrentReading(IntPtr self, GameInputKind inputKind, IntPtr device, IntPtr reading)
        {
            Interlocked.Increment(ref *CallCountSlot(self));
            OnNativeCall?.Invoke();
            if (CurrentReading == IntPtr.Zero)
            {
                *(IntPtr*)reading = IntPtr.Zero;
                return GameInputHResult.ReadingNotFound;
            }

            // 依 COM 慣例，out 參數交出的是呼叫端擁有的新參考。
            _ = AddRef(CurrentReading);
            *(IntPtr*)reading = CurrentReading;
            return 0;
        }

        private static int RegisterDeviceCallback(IntPtr self, IntPtr device, GameInputKind inputKind, IntPtr context, IntPtr callbackFunc, IntPtr callbackToken)
        {
            // 模擬註冊失敗前已進行中的事件：先觸發回呼、再回報失敗，驗證已收集的裝置會被釋放。
            if (EnumeratedDevice != IntPtr.Zero)
            {
                ((delegate* unmanaged[Stdcall]<ulong, IntPtr, GameInputDeviceEvent, IGameInputDevice, void>)callbackFunc)(
                    0,
                    context,
                    GameInputDeviceEvent.GameInputDeviceEventConnected,
                    new IGameInputDevice(EnumeratedDevice));
            }

            *(ulong*)callbackToken = 0;
            return unchecked((int)0x80004005);
        }

        private static int CreateForceFeedbackEffect(IntPtr self, uint motorIndex, IntPtr parameters, IntPtr effect)
        {
            LastEffectKind = Marshal.PtrToStructure<GameInputForceFeedbackParams>(parameters).Kind;
            *(IntPtr*)effect = IntPtr.Zero;
            return GameInputHResult.FeedbackNotSupported;
        }

        private static UIntPtr GetRawData(IntPtr self, UIntPtr bufferSize, IntPtr buffer)
        {
            byte* destination = (byte*)buffer;
            for (ulong index = 0; index < bufferSize.ToUInt64(); index++)
            {
                destination[index] = (byte)(index + 1);
            }

            return bufferSize;
        }

        private static byte SetRawData(IntPtr self, UIntPtr bufferSize, IntPtr buffer)
        {
            byte[] data = new byte[(int)bufferSize.ToUInt64()];
            Marshal.Copy(buffer, data, 0, data.Length);
            LastSetRawData = data;
            return 1;
        }

        private static byte GetGamepadAxisMappingInfo(IntPtr self, GameInputGamepadAxes axisElement, IntPtr mapping)
        {
            Marshal.StructureToPtr(
                new GameInputAxisMapping
                {
                    ControllerElementKind = (GameInputElementKind)2,
                    ControllerIndex = 7,
                    IsInverted = true,
                    FromTwoButtons = false,
                    ButtonMinIndexValue = 3,
                    ReferenceDirection = GameInputSwitchPosition.GameInputSwitchUp
                },
                mapping,
                fDeleteOld: false);
            return 1;
        }

        private static byte GetGamepadState(IntPtr self, IntPtr state)
        {
            *(GameInputGamepadState*)state = new GameInputGamepadState { Buttons = GameInputGamepadButtons.GameInputGamepadA, LeftTrigger = 0.5f };
            return 1;
        }


    }
}
