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
            _ = device.Status;
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

        ObjectDisposedException exception = Assert.ThrowsExactly<ObjectDisposedException>(() => device.Status);

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
    public void DeviceEventQueueIsBoundedAndDropsOldestEvents()
    {
        using FakeComObject fake = FakeComObject.CreateGameInput();
        using GameInputDeviceManager manager = new(new GameInputClient(new GameInputComHandle(fake.Pointer)));
        int total = GameInputDeviceManager.MaxQueuedEvents + 500;

        for (int index = 0; index < total; index++)
        {
            manager.PublishDeviceEvent(new GameInputDeviceManagerEvent(
                (ulong)index,
                GameInputDeviceStatus.GameInputDeviceConnected,
                GameInputDeviceStatus.GameInputDeviceNoStatus,
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
    public void DeviceInfoPointersAreReadWhileTheDeviceIsStillAlive()
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

        Assert.AreEqual("Fake Pad", snapshot.DisplayName, "裝置資訊內的原生指標必須在同一個租約內讀取。");
        Assert.AreEqual(0, fake.RefCount);
    }

    [TestMethod]
    public void NullDeviceInfoPointerThrowsGameInputException()
    {
        using FakeComObject fake = FakeComObject.CreateDevice("Fake Pad");
        using GameInputDevice device = new(new IGameInputDevice(fake.Pointer));
        FakeComObject.ReturnNullDeviceInfo = true;

        try
        {
            GameInputException exception = Assert.ThrowsExactly<GameInputException>(() => device.GetDeviceInfoSnapshot());
            Assert.AreEqual(unchecked((int)0x80004003), exception.HResult);
        }
        finally
        {
            FakeComObject.ReturnNullDeviceInfo = false;
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
    /// 兩個目標框架都適用；委派存放在靜態欄位，確保原生端呼叫期間不會被 GC 回收。
    /// </summary>
    private sealed unsafe class FakeComObject : IDisposable
    {
        public const ulong Timestamp = 42;

        private static readonly RefCountFunction s_addRef = AddRef;
        private static readonly RefCountFunction s_release = Release;
        private static readonly TimestampFunction s_timestamp = GetTimestamp;
        private static readonly DeviceStatusFunction s_deviceStatus = GetDeviceStatus;
        private static readonly GetCurrentReadingFunction s_getCurrentReading = GetCurrentReading;
        private static readonly RegisterDeviceCallbackFunction s_registerDeviceCallback = RegisterDeviceCallback;
        private static readonly GetDeviceInfoFunction s_getDeviceInfo = GetDeviceInfo;
        private static readonly CreateForceFeedbackEffectFunction s_createForceFeedbackEffect = CreateForceFeedbackEffect;
        private static readonly GetRawDataFunction s_getRawData = GetRawData;
        private static readonly SetRawDataFunction s_setRawData = SetRawData;
        private static readonly GetAxisMappingFunction s_getAxisMapping = GetGamepadAxisMappingInfo;
        private static readonly GetGamepadStateFunction s_getGamepadState = GetGamepadState;

        private readonly IntPtr _vtbl;
        private IntPtr _deviceInfo;
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
            *DeviceInfoSlot(Pointer) = IntPtr.Zero;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint RefCountFunction(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetDeviceInfoFunction(IntPtr self, IntPtr info);

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
        private delegate GameInputDeviceStatus DeviceStatusFunction(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetCurrentReadingFunction(IntPtr self, GameInputKind inputKind, IntPtr device, IntPtr reading);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int RegisterDeviceCallbackFunction(IntPtr self, IntPtr device, GameInputKind inputKind, GameInputDeviceStatus statusFilter, GameInputEnumerationKind enumerationKind, IntPtr context, IntPtr callbackFunc, IntPtr callbackToken);

        public static Action? OnNativeCall { get; set; }

        /// <summary>
        /// 參考計數歸零時觸發，用來模擬原生物件釋放後記憶體被回收。
        /// </summary>
        public static Action? OnFinalRelease { get; set; }

        /// <summary>
        /// 為 true 時，GetDeviceInfo 回報成功但不提供資訊指標。
        /// </summary>
        public static bool ReturnNullDeviceInfo { get; set; }

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
            table->GetDeviceStatus = (delegate* unmanaged[Stdcall]<IntPtr, GameInputDeviceStatus>)Marshal.GetFunctionPointerForDelegate(s_deviceStatus);
            table->GetDeviceInfo = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Marshal.GetFunctionPointerForDelegate(s_getDeviceInfo);
            table->CreateForceFeedbackEffect = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, void**, int>)Marshal.GetFunctionPointerForDelegate(s_createForceFeedbackEffect);
            FakeComObject fake = new(vtbl);
            if (displayName is not null)
            {
                byte[] utf8 = Encoding.UTF8.GetBytes(displayName + '\0');
                fake._displayName = Marshal.AllocHGlobal(utf8.Length);
                fake._displayNameLength = utf8.Length - 1;
                Marshal.Copy(utf8, 0, fake._displayName, utf8.Length);
                fake._deviceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<GameInputDeviceInfo>());
                Marshal.StructureToPtr(new GameInputDeviceInfo { DisplayName = fake._displayName }, fake._deviceInfo, fDeleteOld: false);
                *DeviceInfoSlot(fake.Pointer) = fake._deviceInfo;
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
            table->RegisterDeviceCallback = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, GameInputKind, GameInputDeviceStatus, GameInputEnumerationKind, IntPtr, IntPtr, ulong*, int>)Marshal.GetFunctionPointerForDelegate(s_registerDeviceCallback);
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
            if (_deviceInfo != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_deviceInfo);
                Marshal.FreeHGlobal(_displayName);
            }
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

        private static IntPtr* DeviceInfoSlot(IntPtr self)
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

        private static int RegisterDeviceCallback(IntPtr self, IntPtr device, GameInputKind inputKind, GameInputDeviceStatus statusFilter, GameInputEnumerationKind enumerationKind, IntPtr context, IntPtr callbackFunc, IntPtr callbackToken)
        {
            // 模擬阻塞式列舉：先同步觸發回呼、再回報失敗，驗證已收集的裝置會被釋放。
            if (EnumeratedDevice != IntPtr.Zero)
            {
                ((delegate* unmanaged[Stdcall]<ulong, IntPtr, IGameInputDevice, ulong, GameInputDeviceStatus, GameInputDeviceStatus, void>)callbackFunc)(
                    0,
                    context,
                    new IGameInputDevice(EnumeratedDevice),
                    0,
                    GameInputDeviceStatus.GameInputDeviceConnected,
                    GameInputDeviceStatus.GameInputDeviceNoStatus);
            }

            *(ulong*)callbackToken = 0;
            return unchecked((int)0x80004005);
        }

        private static int GetDeviceInfo(IntPtr self, IntPtr info)
        {
            Interlocked.Increment(ref *CallCountSlot(self));
            OnNativeCall?.Invoke();
            *(IntPtr*)info = ReturnNullDeviceInfo ? IntPtr.Zero : *DeviceInfoSlot(self);
            return 0;
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

        private static GameInputDeviceStatus GetDeviceStatus(IntPtr self)
        {
            Interlocked.Increment(ref *CallCountSlot(self));
            OnNativeCall?.Invoke();
            return GameInputDeviceStatus.GameInputDeviceConnected;
        }
    }
}
