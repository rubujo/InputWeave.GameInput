using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput.Benchmarks;

/// <summary>
/// 以假 vtable 測試替身量測 <see cref="GameInputReading"/> 熱路徑的配置與延遲，
/// 不依賴實體硬體，量測結果聚焦在包裝層本身的成本。
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ReadingBenchmarks
{
    private NativeReadingStub _stub = null!;
    private GameInputReading _reading = null!;
    private float[] _axisBuffer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _stub = NativeReadingStub.Create(axes: [0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f]);
        _reading = new GameInputReading(_stub.Native);
        _axisBuffer = new float[8];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _reading.Dispose();
        _stub.Dispose();
    }

    /// <summary>
    /// 每次呼叫配置新陣列的多載，作為零配置多載的比較基準。
    /// </summary>
    /// <returns>配置的軸值陣列。</returns>
    [Benchmark(Baseline = true)]
    public float[] GetControllerAxisStateAllocating()
    {
        return _reading.GetControllerAxisState();
    }

    /// <summary>
    /// 使用既有緩衝區的零配置多載。
    /// </summary>
    /// <returns>原生 API 寫入的元素數。</returns>
    [Benchmark]
    public uint GetControllerAxisStateBuffer()
    {
        return _reading.GetControllerAxisState(_axisBuffer);
    }

    /// <summary>
    /// 快取後的 InputKind 屬性存取（首次呼叫後不再進原生層）。
    /// </summary>
    /// <returns>快取的輸入種類。</returns>
    [Benchmark]
    public GameInputKind InputKindCached()
    {
        return _reading.InputKind;
    }

    /// <summary>
    /// 在原生記憶體中組出最小可用的 IGameInputReading vtable，與測試專案的替身同構，
    /// 讓基準測試不需要實體 GameInput 硬體。
    /// </summary>
    internal sealed unsafe class NativeReadingStub : IDisposable
    {
        [ThreadStatic]
        private static float[]? t_axes;

        private readonly IntPtr _vtbl;
        private readonly IntPtr _object;
        private bool _disposed;

        private NativeReadingStub(IntPtr vtbl, IntPtr nativeObject)
        {
            _vtbl = vtbl;
            _object = nativeObject;
        }

        public IGameInputReading Native => new(_object);

        public static NativeReadingStub Create(float[]? axes = null)
        {
            t_axes = axes ?? [];

            IntPtr vtbl = Marshal.AllocHGlobal(sizeof(IGameInputReadingVtbl));
            *(IGameInputReadingVtbl*)vtbl = default;
            IGameInputReadingVtbl* vtblPtr = (IGameInputReadingVtbl*)vtbl;
            vtblPtr->AddRef = &NoOpRefCount;
            vtblPtr->Release = &NoOpRefCount;
            vtblPtr->GetInputKind = &GetInputKind;
            vtblPtr->GetTimestamp = &GetTimestamp;
            vtblPtr->GetControllerAxisCount = &GetControllerAxisCount;
            vtblPtr->GetControllerAxisState = &GetControllerAxisState;

            IntPtr nativeObject = Marshal.AllocHGlobal(IntPtr.Size);
            *(IntPtr*)nativeObject = vtbl;
            return new NativeReadingStub(vtbl, nativeObject);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Marshal.FreeHGlobal(_object);
            Marshal.FreeHGlobal(_vtbl);
            _disposed = true;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint NoOpRefCount(IntPtr self)
        {
            return 1;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static GameInputKind GetInputKind(IntPtr self)
        {
            return GameInputKind.GameInputKindController;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static ulong GetTimestamp(IntPtr self)
        {
            return 1234;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint GetControllerAxisCount(IntPtr self)
        {
            return (uint)(t_axes?.Length ?? 0);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint GetControllerAxisState(IntPtr self, uint stateArrayCount, IntPtr stateArray)
        {
            float[] source = t_axes ?? [];
            uint count = Math.Min(stateArrayCount, (uint)source.Length);
            float* destination = (float*)stateArray;
            for (int index = 0; index < count; index++)
            {
                destination[index] = source[index];
            }

            return count;
        }
    }
}
