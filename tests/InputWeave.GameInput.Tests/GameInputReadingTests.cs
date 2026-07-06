using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputReadingTests
{
    [TestMethod]
    public void GetControllerAxisStateWithBufferWritesExpectedData()
    {
        using NativeReadingStub stub = NativeReadingStub.Create(axes: [0.1f, 0.2f, 0.3f]);
        using GameInputReading reading = new(stub.Native);
        float[] buffer = new float[5];

        uint written = reading.GetControllerAxisState(buffer);

        Assert.AreEqual(3u, written);
        Assert.AreEqual(0.1f, buffer[0]);
        Assert.AreEqual(0.2f, buffer[1]);
        Assert.AreEqual(0.3f, buffer[2]);
        Assert.AreEqual(0f, buffer[3]);
    }

    [TestMethod]
    public void GetControllerButtonStateWithBufferWritesExpectedData()
    {
        using NativeReadingStub stub = NativeReadingStub.Create(buttons: [1, 0, 1]);
        using GameInputReading reading = new(stub.Native);
        byte[] buffer = new byte[5];

        uint written = reading.GetControllerButtonState(buffer);

        Assert.AreEqual(3u, written);
        Assert.AreEqual(1, buffer[0]);
        Assert.AreEqual(0, buffer[1]);
        Assert.AreEqual(1, buffer[2]);
        Assert.AreEqual(0, buffer[3]);
    }

    [TestMethod]
    public void GetControllerSwitchStateWithBufferWritesExpectedData()
    {
        using NativeReadingStub stub = NativeReadingStub.Create(
            switches: [GameInputSwitchPosition.GameInputSwitchUp, GameInputSwitchPosition.GameInputSwitchDown]);
        using GameInputReading reading = new(stub.Native);
        GameInputSwitchPosition[] buffer = new GameInputSwitchPosition[4];

        uint written = reading.GetControllerSwitchState(buffer);

        Assert.AreEqual(2u, written);
        Assert.AreEqual(GameInputSwitchPosition.GameInputSwitchUp, buffer[0]);
        Assert.AreEqual(GameInputSwitchPosition.GameInputSwitchDown, buffer[1]);
        Assert.AreEqual(GameInputSwitchPosition.GameInputSwitchCenter, buffer[2]);
    }

    [TestMethod]
    public void GetKeyStateWithBufferWritesExpectedData()
    {
        using NativeReadingStub stub = NativeReadingStub.Create(keys:
        [
            new GameInputKeyState { ScanCode = 10, VirtualKey = 65 },
            new GameInputKeyState { ScanCode = 11, VirtualKey = 66 }
        ]);
        using GameInputReading reading = new(stub.Native);
        GameInputKeyState[] buffer = new GameInputKeyState[4];

        uint written = reading.GetKeyState(buffer);

        Assert.AreEqual(2u, written);
        Assert.AreEqual(10u, buffer[0].ScanCode);
        Assert.AreEqual(65u, buffer[0].VirtualKey);
        Assert.AreEqual(11u, buffer[1].ScanCode);
        Assert.AreEqual(66u, buffer[1].VirtualKey);
        Assert.AreEqual(0u, buffer[2].ScanCode);
    }

    /// <summary>
    /// 在原生記憶體中組出最小可用的 IGameInputReading vtable，供單元測試在沒有真實 GameInput 硬體時
    /// 驗證 <see cref="GameInputReading"/> 對陣列緩衝區方法的釘選與轉呼叫行為。
    /// </summary>
    private sealed unsafe class NativeReadingStub : IDisposable
    {
        [ThreadStatic]
        private static float[]? t_axes;

        [ThreadStatic]
        private static byte[]? t_buttons;

        [ThreadStatic]
        private static GameInputSwitchPosition[]? t_switches;

        [ThreadStatic]
        private static GameInputKeyState[]? t_keys;

        private readonly IntPtr _vtbl;
        private readonly IntPtr _object;
        private bool _disposed;

        private NativeReadingStub(IntPtr vtbl, IntPtr nativeObject)
        {
            _vtbl = vtbl;
            _object = nativeObject;
        }

        public IGameInputReading Native => new(_object);

        public static NativeReadingStub Create(
            float[]? axes = null,
            byte[]? buttons = null,
            GameInputSwitchPosition[]? switches = null,
            GameInputKeyState[]? keys = null)
        {
            t_axes = axes ?? [];
            t_buttons = buttons ?? [];
            t_switches = switches ?? [];
            t_keys = keys ?? [];

            IntPtr vtbl = Marshal.AllocHGlobal(sizeof(IGameInputReadingVtbl));
            *(IGameInputReadingVtbl*)vtbl = default;
            IGameInputReadingVtbl* vtblPtr = (IGameInputReadingVtbl*)vtbl;
            vtblPtr->AddRef = &NoOpRefCount;
            vtblPtr->Release = &NoOpRefCount;
            vtblPtr->GetControllerAxisCount = &GetControllerAxisCount;
            vtblPtr->GetControllerAxisState = &GetControllerAxisState;
            vtblPtr->GetControllerButtonCount = &GetControllerButtonCount;
            vtblPtr->GetControllerButtonState = &GetControllerButtonState;
            vtblPtr->GetControllerSwitchCount = &GetControllerSwitchCount;
            vtblPtr->GetControllerSwitchState = &GetControllerSwitchState;
            vtblPtr->GetKeyCount = &GetKeyCount;
            vtblPtr->GetKeyState = &GetKeyState;

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

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint GetControllerButtonCount(IntPtr self)
        {
            return (uint)(t_buttons?.Length ?? 0);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint GetControllerButtonState(IntPtr self, uint stateArrayCount, IntPtr stateArray)
        {
            byte[] source = t_buttons ?? [];
            uint count = Math.Min(stateArrayCount, (uint)source.Length);
            byte* destination = (byte*)stateArray;
            for (int index = 0; index < count; index++)
            {
                destination[index] = source[index];
            }

            return count;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint GetControllerSwitchCount(IntPtr self)
        {
            return (uint)(t_switches?.Length ?? 0);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint GetControllerSwitchState(IntPtr self, uint stateArrayCount, IntPtr stateArray)
        {
            GameInputSwitchPosition[] source = t_switches ?? [];
            uint count = Math.Min(stateArrayCount, (uint)source.Length);
            GameInputSwitchPosition* destination = (GameInputSwitchPosition*)stateArray;
            for (int index = 0; index < count; index++)
            {
                destination[index] = source[index];
            }

            return count;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint GetKeyCount(IntPtr self)
        {
            return (uint)(t_keys?.Length ?? 0);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint GetKeyState(IntPtr self, uint stateArrayCount, IntPtr stateArray)
        {
            GameInputKeyState[] source = t_keys ?? [];
            uint count = Math.Min(stateArrayCount, (uint)source.Length);
            GameInputKeyState* destination = (GameInputKeyState*)stateArray;
            for (int index = 0; index < count; index++)
            {
                destination[index] = source[index];
            }

            return count;
        }
    }
}
