using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

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

        private readonly IntPtr _vtbl;
        private bool _disposed;

        private FakeComObject(IntPtr vtbl)
        {
            _vtbl = vtbl;
            Pointer = Marshal.AllocHGlobal(IntPtr.Size + (2 * sizeof(int)));
            *(IntPtr*)Pointer = vtbl;
            *RefCountSlot(Pointer) = 1;
            *CallCountSlot(Pointer) = 0;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint RefCountFunction(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate ulong TimestampFunction(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate GameInputDeviceStatus DeviceStatusFunction(IntPtr self);

        public static Action? OnNativeCall { get; set; }

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

        public static FakeComObject CreateDevice()
        {
            IntPtr vtbl = AllocateVtbl(sizeof(IGameInputDeviceVtbl));
            IGameInputDeviceVtbl* table = (IGameInputDeviceVtbl*)vtbl;
            table->AddRef = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_addRef);
            table->Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_release);
            table->GetDeviceStatus = (delegate* unmanaged[Stdcall]<IntPtr, GameInputDeviceStatus>)Marshal.GetFunctionPointerForDelegate(s_deviceStatus);
            return new FakeComObject(vtbl);
        }

        public static FakeComObject CreateReading()
        {
            IntPtr vtbl = AllocateVtbl(sizeof(IGameInputReadingVtbl));
            IGameInputReadingVtbl* table = (IGameInputReadingVtbl*)vtbl;
            table->AddRef = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_addRef);
            table->Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_release);
            table->GetTimestamp = (delegate* unmanaged[Stdcall]<IntPtr, ulong>)Marshal.GetFunctionPointerForDelegate(s_timestamp);
            return new FakeComObject(vtbl);
        }

        public static FakeComObject CreateGameInput()
        {
            IntPtr vtbl = AllocateVtbl(sizeof(IGameInputVtbl));
            IGameInputVtbl* table = (IGameInputVtbl*)vtbl;
            table->AddRef = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_addRef);
            table->Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_release);
            table->GetCurrentTimestamp = (delegate* unmanaged[Stdcall]<IntPtr, ulong>)Marshal.GetFunctionPointerForDelegate(s_timestamp);
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
            return (uint)Interlocked.Decrement(ref *RefCountSlot(self));
        }

        private static ulong GetTimestamp(IntPtr self)
        {
            Interlocked.Increment(ref *CallCountSlot(self));
            OnNativeCall?.Invoke();
            return Timestamp;
        }

        private static GameInputDeviceStatus GetDeviceStatus(IntPtr self)
        {
            Interlocked.Increment(ref *CallCountSlot(self));
            OnNativeCall?.Invoke();
            return GameInputDeviceStatus.GameInputDeviceConnected;
        }
    }
}
