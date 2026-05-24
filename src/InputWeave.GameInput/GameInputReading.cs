using System;
using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput
{
    /// <summary>
    /// GameInput 低階讀取資料包裝。
    /// </summary>
    public sealed class GameInputReading : IDisposable
    {
        private IGameInputReading? _native;
        private bool _disposed;

        internal GameInputReading(IGameInputReading native)
        {
            _native = native;
        }

        internal IGameInputReading NativeInterface
        {
            get
            {
                return Native;
            }
        }

        /// <summary>
        /// 讀取資料包含的輸入種類。
        /// </summary>
        public GameInputKind InputKind
        {
            get
            {
                return Native.GetInputKind();
            }
        }

        /// <summary>
        /// 讀取資料的 GameInput 時間戳記。
        /// </summary>
        public ulong Timestamp
        {
            get
            {
                return Native.GetTimestamp();
            }
        }

        /// <summary>
        /// 取得 reading 所屬裝置。
        /// </summary>
        public GameInputDevice? GetDevice()
        {
            Native.GetDevice(out IGameInputDevice? device);
            return device is null ? null : new GameInputDevice(device);
        }

        /// <summary>
        /// 取得 controller 軸狀態。
        /// </summary>
        public float[] GetControllerAxisState()
        {
            uint count = Native.GetControllerAxisCount();
            if (count == 0)
            {
                return Array.Empty<float>();
            }

            float[] state = new float[checked((int)count)];
            uint written = Native.GetControllerAxisState(count, state);
            if (written == count)
            {
                return state;
            }

            Array.Resize(ref state, checked((int)written));
            return state;
        }

        /// <summary>
        /// 取得 controller 按鈕狀態。
        /// </summary>
        public bool[] GetControllerButtonState()
        {
            uint count = Native.GetControllerButtonCount();
            if (count == 0)
            {
                return Array.Empty<bool>();
            }

            byte[] nativeState = new byte[checked((int)count)];
            uint written = Native.GetControllerButtonState(count, nativeState);
            bool[] state = new bool[checked((int)written)];
            for (int index = 0; index < state.Length; index++)
            {
                state[index] = nativeState[index] != 0;
            }

            return state;
        }

        /// <summary>
        /// 取得 controller switch 狀態。
        /// </summary>
        public GameInputSwitchPosition[] GetControllerSwitchState()
        {
            uint count = Native.GetControllerSwitchCount();
            if (count == 0)
            {
                return Array.Empty<GameInputSwitchPosition>();
            }

            GameInputSwitchPosition[] state = new GameInputSwitchPosition[checked((int)count)];
            uint written = Native.GetControllerSwitchState(count, state);
            if (written == count)
            {
                return state;
            }

            Array.Resize(ref state, checked((int)written));
            return state;
        }

        /// <summary>
        /// 取得鍵盤按鍵狀態。
        /// </summary>
        public GameInputKeyState[] GetKeyState()
        {
            uint count = Native.GetKeyCount();
            if (count == 0)
            {
                return Array.Empty<GameInputKeyState>();
            }

            GameInputKeyState[] state = new GameInputKeyState[checked((int)count)];
            uint written = Native.GetKeyState(count, state);
            if (written == count)
            {
                return state;
            }

            Array.Resize(ref state, checked((int)written));
            return state;
        }

        /// <summary>
        /// 嘗試讀取 gamepad 狀態。
        /// </summary>
        /// <param name="state">成功時取得 gamepad 狀態。</param>
        /// <returns>若讀取資料包含 gamepad 狀態，傳回 <see langword="true"/>。</returns>
        public bool TryGetGamepadState(out GameInputGamepadState state)
        {
            return Native.GetGamepadState(out state);
        }

        /// <summary>
        /// 嘗試讀取滑鼠狀態。
        /// </summary>
        /// <param name="state">成功時取得滑鼠狀態。</param>
        /// <returns>若讀取資料包含滑鼠狀態，傳回 <see langword="true"/>。</returns>
        public bool TryGetMouseState(out GameInputMouseState state)
        {
            return Native.GetMouseState(out state);
        }

        /// <summary>
        /// 嘗試讀取感測器狀態。
        /// </summary>
        public bool TryGetSensorsState(out GameInputSensorsState state)
        {
            return Native.GetSensorsState(out state);
        }

        /// <summary>
        /// 嘗試讀取 arcade stick 狀態。
        /// </summary>
        public bool TryGetArcadeStickState(out GameInputArcadeStickState state)
        {
            return Native.GetArcadeStickState(out state);
        }

        /// <summary>
        /// 嘗試讀取 flight stick 狀態。
        /// </summary>
        public bool TryGetFlightStickState(out GameInputFlightStickState state)
        {
            return Native.GetFlightStickState(out state);
        }

        /// <summary>
        /// 嘗試讀取 racing wheel 狀態。
        /// </summary>
        public bool TryGetRacingWheelState(out GameInputRacingWheelState state)
        {
            return Native.GetRacingWheelState(out state);
        }

        /// <summary>
        /// 嘗試取得 raw report。
        /// </summary>
        public bool TryGetRawReport(out GameInputRawDeviceReport? report)
        {
            if (Native.GetRawReport(out IGameInputRawDeviceReport? nativeReport) && nativeReport is not null)
            {
                report = new GameInputRawDeviceReport(nativeReport);
                return true;
            }

            report = null;
            return false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_native is not null)
            {
                Marshal.ReleaseComObject(_native);
                _native = null;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private IGameInputReading Native
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(GameInputReading));
                }

                return _native ?? throw new ObjectDisposedException(nameof(GameInputReading));
            }
        }
    }
}
