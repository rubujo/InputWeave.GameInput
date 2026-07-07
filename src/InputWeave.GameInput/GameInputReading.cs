using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// A GameInput low-level reading wrapper.
/// GameInput 低階讀取資料包裝。
/// </summary>
public sealed class GameInputReading : IDisposable
{
    private IGameInputReading? _native;
    private bool _disposed;
    private GameInputKind? _cachedInputKind;
    private ulong? _cachedTimestamp;

#if NET10_0_OR_GREATER
    private readonly System.Threading.Lock _cacheSyncRoot = new();
#else
    private readonly object _cacheSyncRoot = new();
#endif

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
    /// The input kinds contained in the reading.
    /// 讀取資料包含的輸入種類。
    /// </summary>
    /// <remarks>
    /// The input kind of a single reading is fixed for its lifetime, so this property caches the result on the same
    /// <see cref="GameInputReading"/> instance to avoid repeated native calls.
    /// 同一筆讀取資料的輸入種類在其存續期間為固定值，此屬性會在同一個 <see cref="GameInputReading"/>
    /// 執行個體上快取結果，避免重複的原生呼叫。
    /// </remarks>
    public GameInputKind InputKind
    {
        get
        {
            if (_cachedInputKind is { } cached)
            {
                return cached;
            }

            lock (_cacheSyncRoot)
            {
                return _cachedInputKind ??= Native.GetInputKind();
            }
        }
    }

    /// <summary>
    /// The GameInput timestamp of the reading.
    /// 讀取資料的 GameInput 時間戳記。
    /// </summary>
    /// <remarks>
    /// The timestamp of a single reading is fixed for its lifetime, so this property caches the result on the same
    /// <see cref="GameInputReading"/> instance to avoid repeated native calls.
    /// 同一筆讀取資料的時間戳記在其存續期間為固定值，此屬性會在同一個 <see cref="GameInputReading"/>
    /// 執行個體上快取結果，避免重複的原生呼叫。
    /// </remarks>
    public ulong Timestamp
    {
        get
        {
            if (_cachedTimestamp is { } cached)
            {
                return cached;
            }

            lock (_cacheSyncRoot)
            {
                return _cachedTimestamp ??= Native.GetTimestamp();
            }
        }
    }

    /// <summary>
    /// Gets the device that owns the reading.
    /// 取得 reading 所屬裝置。
    /// </summary>
    /// <returns>The owning device wrapper, or null when unavailable. 所屬裝置包裝；無法取得時為 null。</returns>
    public GameInputDevice? GetDevice()
    {
        Native.GetDevice(out IGameInputDevice? device);
        return device is { } deviceValue ? new GameInputDevice(deviceValue) : null;
    }

    /// <summary>
    /// Gets the controller axis state.
    /// 取得 controller 軸狀態。
    /// </summary>
    /// <returns>A new array containing the controller axis values. 包含 controller 軸值的新陣列。</returns>
    /// <exception cref="InvalidOperationException">The element count reported by the native side exceeds the internal limit, which is treated as an anomalous device or driver report. 原生回報的元素數量超過內部上限，視為裝置或驅動程式回報異常。</exception>
    public float[] GetControllerAxisState()
    {
        uint count = Native.GetControllerAxisCount();
        if (count == 0)
        {
            return [];
        }

        float[] state = new float[NativeSizeGuard.EnsureCount(count, NativeSizeGuard.MaxElementCount, "controller 軸數量")];
#if NET10_0_OR_GREATER
        uint written = InvokeArrayState(state, Native.GetControllerAxisState);
#else
        uint written = Native.GetControllerAxisState(count, state);
#endif
        written = Math.Min(written, count);
        if (written == count)
        {
            return state;
        }

        Array.Resize(ref state, (int)written);
        return state;
    }

    /// <summary>
    /// Gets the controller axis state using an existing array buffer, avoiding per-frame allocation.
    /// 使用既有陣列緩衝區取得 controller 軸狀態，避免每影格分配。
    /// </summary>
    /// <param name="stateArray">The managed array that receives the state data. 接收狀態資料的 managed 陣列。</param>
    /// <returns>The number of elements written by the native API. 原生 API 寫入的元素數。</returns>
    public uint GetControllerAxisState(float[] stateArray)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(stateArray);
#else
        if (stateArray is null)
        {
            throw new ArgumentNullException(nameof(stateArray));
        }
#endif
#if NET10_0_OR_GREATER
        return InvokeArrayState(stateArray, Native.GetControllerAxisState);
#else
        return Native.GetControllerAxisState((uint)stateArray.Length, stateArray);
#endif
    }

    /// <summary>
    /// Gets the controller button state.
    /// 取得 controller 按鈕狀態。
    /// </summary>
    /// <returns>A new array containing the controller button states. 包含 controller 按鈕狀態的新陣列。</returns>
    /// <exception cref="InvalidOperationException">The element count reported by the native side exceeds the internal limit, which is treated as an anomalous device or driver report. 原生回報的元素數量超過內部上限，視為裝置或驅動程式回報異常。</exception>
    public bool[] GetControllerButtonState()
    {
        uint count = Native.GetControllerButtonCount();
        if (count == 0)
        {
            return [];
        }

        byte[] nativeState = new byte[NativeSizeGuard.EnsureCount(count, NativeSizeGuard.MaxElementCount, "controller 按鈕數量")];
#if NET10_0_OR_GREATER
        uint written = InvokeArrayState(nativeState, Native.GetControllerButtonState);
#else
        uint written = Native.GetControllerButtonState(count, nativeState);
#endif
        written = Math.Min(written, count);
        bool[] state = new bool[(int)written];
        for (int index = 0; index < state.Length; index++)
        {
            state[index] = nativeState[index] != 0;
        }

        return state;
    }

    /// <summary>
    /// Gets the controller button state using an existing byte array buffer, avoiding per-frame allocation.
    /// 使用既有 byte 陣列緩衝區取得 controller 按鈕狀態，避免每影格分配。
    /// </summary>
    /// <param name="stateArray">The managed byte array that receives the state data. 接收狀態資料的 managed byte 陣列。</param>
    /// <returns>The number of elements written by the native API. 原生 API 寫入的元素數。</returns>
    public uint GetControllerButtonState(byte[] stateArray)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(stateArray);
#else
        if (stateArray is null)
        {
            throw new ArgumentNullException(nameof(stateArray));
        }
#endif
#if NET10_0_OR_GREATER
        return InvokeArrayState(stateArray, Native.GetControllerButtonState);
#else
        return Native.GetControllerButtonState((uint)stateArray.Length, stateArray);
#endif
    }

    /// <summary>
    /// Gets the controller switch state.
    /// 取得 controller switch 狀態。
    /// </summary>
    /// <returns>A new array containing the controller switch positions. 包含 controller 開關位置的新陣列。</returns>
    /// <exception cref="InvalidOperationException">The element count reported by the native side exceeds the internal limit, which is treated as an anomalous device or driver report. 原生回報的元素數量超過內部上限，視為裝置或驅動程式回報異常。</exception>
    public GameInputSwitchPosition[] GetControllerSwitchState()
    {
        uint count = Native.GetControllerSwitchCount();
        if (count == 0)
        {
            return [];
        }

        GameInputSwitchPosition[] state = new GameInputSwitchPosition[NativeSizeGuard.EnsureCount(count, NativeSizeGuard.MaxElementCount, "controller 開關數量")];
#if NET10_0_OR_GREATER
        uint written = InvokeArrayState(state, Native.GetControllerSwitchState);
#else
        uint written = Native.GetControllerSwitchState(count, state);
#endif
        written = Math.Min(written, count);
        if (written == count)
        {
            return state;
        }

        Array.Resize(ref state, (int)written);
        return state;
    }

    /// <summary>
    /// Gets the controller switch state using an existing array buffer, avoiding per-frame allocation.
    /// 使用既有陣列緩衝區取得 controller switch 狀態，避免每影格分配。
    /// </summary>
    /// <param name="stateArray">The managed array that receives the state data. 接收狀態資料的 managed 陣列。</param>
    /// <returns>The number of elements written by the native API. 原生 API 寫入的元素數。</returns>
    public uint GetControllerSwitchState(GameInputSwitchPosition[] stateArray)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(stateArray);
#else
        if (stateArray is null)
        {
            throw new ArgumentNullException(nameof(stateArray));
        }
#endif
#if NET10_0_OR_GREATER
        return InvokeArrayState(stateArray, Native.GetControllerSwitchState);
#else
        return Native.GetControllerSwitchState((uint)stateArray.Length, stateArray);
#endif
    }

    /// <summary>
    /// Gets the keyboard key state.
    /// 取得鍵盤按鍵狀態。
    /// </summary>
    /// <returns>A new array containing the active key states. 包含作用中按鍵狀態的新陣列。</returns>
    /// <exception cref="InvalidOperationException">The element count reported by the native side exceeds the internal limit, which is treated as an anomalous device or driver report. 原生回報的元素數量超過內部上限，視為裝置或驅動程式回報異常。</exception>
    public GameInputKeyState[] GetKeyState()
    {
        uint count = Native.GetKeyCount();
        if (count == 0)
        {
            return [];
        }

        GameInputKeyState[] state = new GameInputKeyState[NativeSizeGuard.EnsureCount(count, NativeSizeGuard.MaxElementCount, "鍵盤按鍵數量")];
#if NET10_0_OR_GREATER
        uint written = InvokeArrayState(state, Native.GetKeyState);
#else
        uint written = Native.GetKeyState(count, state);
#endif
        written = Math.Min(written, count);
        if (written == count)
        {
            return state;
        }

        Array.Resize(ref state, (int)written);
        return state;
    }

    /// <summary>
    /// Gets the keyboard key state using an existing array buffer, avoiding per-frame allocation.
    /// 使用既有陣列緩衝區取得鍵盤按鍵狀態，避免每影格分配。
    /// </summary>
    /// <param name="stateArray">The managed array that receives the state data. 接收狀態資料的 managed 陣列。</param>
    /// <returns>The number of elements written by the native API. 原生 API 寫入的元素數。</returns>
    public uint GetKeyState(GameInputKeyState[] stateArray)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(stateArray);
#else
        if (stateArray is null)
        {
            throw new ArgumentNullException(nameof(stateArray));
        }
#endif
#if NET10_0_OR_GREATER
        return InvokeArrayState(stateArray, Native.GetKeyState);
#else
        return Native.GetKeyState((uint)stateArray.Length, stateArray);
#endif
    }

    /// <summary>
    /// Tries to read the gamepad state.
    /// 嘗試讀取 gamepad 狀態。
    /// </summary>
    /// <param name="state">The output field that receives the state data. 接收狀態資料的輸出欄位。</param>
    /// <returns>Returns true when the reading contains gamepad state; otherwise returns false. 若 reading 包含 gamepad 狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetGamepadState(out GameInputGamepadState state)
    {
        return Native.GetGamepadState(out state);
    }

    /// <summary>
    /// Tries to create a gamepad snapshot that holds no native lifetime.
    /// 嘗試建立不持有原生生命週期的 gamepad 快照。
    /// </summary>
    /// <param name="snapshot">Receives the gamepad snapshot on success. 成功時接收 gamepad 快照。</param>
    /// <returns>Returns true when the reading contains gamepad state; otherwise returns false. 若 reading 包含 gamepad 狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetGamepadSnapshot(out GamepadReadingSnapshot? snapshot)
    {
        if (TryGetGamepadState(out GameInputGamepadState state))
        {
            snapshot = new GamepadReadingSnapshot(Timestamp, state);
            return true;
        }

        snapshot = null;
        return false;
    }

    /// <summary>
    /// Tries to read the mouse state.
    /// 嘗試讀取滑鼠狀態。
    /// </summary>
    /// <param name="state">The output field that receives the state data. 接收狀態資料的輸出欄位。</param>
    /// <returns>Returns true when the reading contains mouse state; otherwise returns false. 若 reading 包含滑鼠狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetMouseState(out GameInputMouseState state)
    {
        return Native.GetMouseState(out state);
    }

    /// <summary>
    /// Tries to create a mouse snapshot that holds no native lifetime.
    /// 嘗試建立不持有原生生命週期的滑鼠快照。
    /// </summary>
    /// <param name="snapshot">Receives the mouse snapshot on success. 成功時接收滑鼠快照。</param>
    /// <returns>Returns true when the reading contains mouse state; otherwise returns false. 若 reading 包含滑鼠狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetMouseSnapshot(out MouseReadingSnapshot? snapshot)
    {
        if (TryGetMouseState(out GameInputMouseState state))
        {
            snapshot = new MouseReadingSnapshot(Timestamp, state);
            return true;
        }

        snapshot = null;
        return false;
    }

    /// <summary>
    /// Tries to read the sensors state.
    /// 嘗試讀取感測器狀態。
    /// </summary>
    /// <param name="state">The output field that receives the state data. 接收狀態資料的輸出欄位。</param>
    /// <returns>Returns true when the reading contains sensors state; otherwise returns false. 若 reading 包含感測器狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetSensorsState(out GameInputSensorsState state)
    {
        return Native.GetSensorsState(out state);
    }

    /// <summary>
    /// Tries to create a sensors snapshot that holds no native lifetime.
    /// 嘗試建立不持有原生生命週期的感測器快照。
    /// </summary>
    /// <param name="snapshot">Receives the sensors snapshot on success. 成功時接收感測器快照。</param>
    /// <returns>Returns true when the reading contains sensors state; otherwise returns false. 若 reading 包含感測器狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetSensorsSnapshot(out SensorsReadingSnapshot? snapshot)
    {
        if (TryGetSensorsState(out GameInputSensorsState state))
        {
            snapshot = new SensorsReadingSnapshot(Timestamp, state);
            return true;
        }

        snapshot = null;
        return false;
    }

    /// <summary>
    /// Tries to read the arcade stick state.
    /// 嘗試讀取 arcade stick 狀態。
    /// </summary>
    /// <param name="state">The output field that receives the state data. 接收狀態資料的輸出欄位。</param>
    /// <returns>Returns true when the reading contains arcade stick state; otherwise returns false. 若 reading 包含 arcade stick 狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetArcadeStickState(out GameInputArcadeStickState state)
    {
        return Native.GetArcadeStickState(out state);
    }

    /// <summary>
    /// Tries to create an arcade stick snapshot that holds no native lifetime.
    /// 嘗試建立不持有原生生命週期的 arcade stick 快照。
    /// </summary>
    /// <param name="snapshot">Receives the arcade stick snapshot on success. 成功時接收 arcade stick 快照。</param>
    /// <returns>Returns true when the reading contains arcade stick state; otherwise returns false. 若 reading 包含 arcade stick 狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetArcadeStickSnapshot(out ArcadeStickReadingSnapshot? snapshot)
    {
        if (TryGetArcadeStickState(out GameInputArcadeStickState state))
        {
            snapshot = new ArcadeStickReadingSnapshot(Timestamp, state);
            return true;
        }

        snapshot = null;
        return false;
    }

    /// <summary>
    /// Tries to read the flight stick state.
    /// 嘗試讀取 flight stick 狀態。
    /// </summary>
    /// <param name="state">The output field that receives the state data. 接收狀態資料的輸出欄位。</param>
    /// <returns>Returns true when the reading contains flight stick state; otherwise returns false. 若 reading 包含 flight stick 狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetFlightStickState(out GameInputFlightStickState state)
    {
        return Native.GetFlightStickState(out state);
    }

    /// <summary>
    /// Tries to create a flight stick snapshot that holds no native lifetime.
    /// 嘗試建立不持有原生生命週期的 flight stick 快照。
    /// </summary>
    /// <param name="snapshot">Receives the flight stick snapshot on success. 成功時接收 flight stick 快照。</param>
    /// <returns>Returns true when the reading contains flight stick state; otherwise returns false. 若 reading 包含 flight stick 狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetFlightStickSnapshot(out FlightStickReadingSnapshot? snapshot)
    {
        if (TryGetFlightStickState(out GameInputFlightStickState state))
        {
            snapshot = new FlightStickReadingSnapshot(Timestamp, state);
            return true;
        }

        snapshot = null;
        return false;
    }

    /// <summary>
    /// Tries to read the racing wheel state.
    /// 嘗試讀取 racing wheel 狀態。
    /// </summary>
    /// <param name="state">The output field that receives the state data. 接收狀態資料的輸出欄位。</param>
    /// <returns>Returns true when the reading contains racing wheel state; otherwise returns false. 若 reading 包含 racing wheel 狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetRacingWheelState(out GameInputRacingWheelState state)
    {
        return Native.GetRacingWheelState(out state);
    }

    /// <summary>
    /// Tries to create a racing wheel snapshot that holds no native lifetime.
    /// 嘗試建立不持有原生生命週期的 racing wheel 快照。
    /// </summary>
    /// <param name="snapshot">Receives the racing wheel snapshot on success. 成功時接收 racing wheel 快照。</param>
    /// <returns>Returns true when the reading contains racing wheel state; otherwise returns false. 若 reading 包含 racing wheel 狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetRacingWheelSnapshot(out RacingWheelReadingSnapshot? snapshot)
    {
        if (TryGetRacingWheelState(out GameInputRacingWheelState state))
        {
            snapshot = new RacingWheelReadingSnapshot(Timestamp, state);
            return true;
        }

        snapshot = null;
        return false;
    }

    /// <summary>
    /// Tries to get the raw report.
    /// 嘗試取得 raw report。
    /// </summary>
    /// <param name="report">The raw device report to send or operate on. 要傳送或操作的 raw device report。</param>
    /// <returns>Returns true when the reading contains a raw report; otherwise returns false. 若 reading 包含 raw report，傳回 true；否則傳回 false。</returns>
    public bool TryGetRawReport(out GameInputRawDeviceReport? report)
    {
        if (Native.GetRawReport(out IGameInputRawDeviceReport? nativeReport) && nativeReport is { } nativeReportValue)
        {
            report = new GameInputRawDeviceReport(nativeReportValue);
            return true;
        }

        report = null;
        return false;
    }

    /// <summary>
    /// Tries to create a keyboard snapshot that holds no native lifetime.
    /// 嘗試建立不持有原生生命週期的 keyboard 快照。
    /// </summary>
    /// <param name="snapshot">Receives the keyboard snapshot on success. 成功時接收 keyboard 快照。</param>
    /// <returns>若 reading 包含 keyboard 狀態，傳回 true；否則傳回 false。原生回報的元素數量超過內部上限時
    /// 也視為讀取失敗，傳回 false 而非拋出例外。</returns>
    public bool TryGetKeyboardSnapshot(out KeyboardReadingSnapshot? snapshot)
    {
        if (!HasAnyInputKind(GameInputKind.GameInputKindKeyboard))
        {
            snapshot = null;
            return false;
        }

        try
        {
            snapshot = new KeyboardReadingSnapshot(Timestamp, GetKeyState());
            return true;
        }
        catch (InvalidOperationException ex) when (ex is not ObjectDisposedException)
        {
            // 原生回報的按鍵數量超過 NativeSizeGuard 上限，視為讀取失敗回傳 false，
            // 維持 Try 方法不拋例外的語意；ObjectDisposedException 屬於呼叫端程式錯誤，仍往外拋。
            snapshot = null;
            return false;
        }
    }

    /// <summary>
    /// Tries to create a controller snapshot that holds no native lifetime.
    /// 嘗試建立不持有原生生命週期的 controller 快照。
    /// </summary>
    /// <param name="snapshot">Receives the controller snapshot on success. 成功時接收 controller 快照。</param>
    /// <returns>若 reading 包含 controller 狀態，傳回 true；否則傳回 false。原生回報的元素數量超過內部上限時
    /// 也視為讀取失敗，傳回 false 而非拋出例外。</returns>
    public bool TryGetControllerSnapshot(out ControllerReadingSnapshot? snapshot)
    {
        if (!HasAnyInputKind(GameInputKind.GameInputKindController))
        {
            snapshot = null;
            return false;
        }

        try
        {
            snapshot = new ControllerReadingSnapshot(
                Timestamp,
                GetControllerAxisState(),
                GetControllerButtonState(),
                GetControllerSwitchState());
            return true;
        }
        catch (InvalidOperationException ex) when (ex is not ObjectDisposedException)
        {
            // 原生回報的元素數量超過 NativeSizeGuard 上限，視為讀取失敗回傳 false，
            // 維持 Try 方法不拋例外的語意；ObjectDisposedException 屬於呼叫端程式錯誤，仍往外拋。
            snapshot = null;
            return false;
        }
    }

    /// <summary>
    /// Tries to create a snapshot that holds no native raw report lifetime.
    /// 嘗試建立不持有原生 raw report 生命週期的快照。
    /// </summary>
    /// <param name="snapshot">Receives the raw report snapshot on success. 成功時接收 raw report 快照。</param>
    /// <returns>若 reading 包含 raw report，傳回 true；否則傳回 false。原生回報的 raw report 大小超過
    /// <see cref="GameInputRawDeviceReport.MaxRawDataSize"/> 時也視為讀取失敗，傳回 false 而非拋出例外。</returns>
    public bool TryGetRawReportSnapshot(out RawDeviceReportSnapshot? snapshot)
    {
        if (!TryGetRawReport(out GameInputRawDeviceReport? report))
        {
            snapshot = null;
            return false;
        }

        using (report)
        {
            try
            {
                snapshot = new RawDeviceReportSnapshot(Timestamp, report!.GetReportInfo(), report.GetRawData());
                return true;
            }
            catch (InvalidOperationException ex) when (ex is not ObjectDisposedException)
            {
                // 原生回報的 raw report 大小超過 GameInputRawDeviceReport.MaxRawDataSize，
                // 視為讀取失敗回傳 false，維持 Try 方法不拋例外的語意；
                // ObjectDisposedException 屬於呼叫端程式錯誤，仍往外拋。
                snapshot = null;
                return false;
            }
        }
    }

    /// <summary>
    /// Releases the COM reference held by the reading wrapper.
    /// 釋放 reading 包裝持有的 COM 參考。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_native is not null)
        {
#if NET10_0_OR_GREATER
            _native.Value.Release();
#else
            if (Marshal.IsComObject(_native))
            {
                _ = Marshal.ReleaseComObject(_native);
            }
#endif
            _native = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private IGameInputReading Native
    {
        get
        {
            return _disposed
                ? throw new ObjectDisposedException(nameof(GameInputReading))
                : _native ?? throw new ObjectDisposedException(nameof(GameInputReading));
        }
    }

#if NET10_0_OR_GREATER
    /// <summary>
    /// Pins the managed array and calls the bare vtable method with the native buffer pointer.
    /// 釘選 managed 陣列並以原生緩衝區指標呼叫裸 vtable 方法。
    /// </summary>
    private static unsafe uint InvokeArrayState<T>(T[] array, Func<uint, IntPtr, uint> invoke)
        where T : unmanaged
    {
        fixed (T* pointer = array)
        {
            return invoke((uint)array.Length, (IntPtr)pointer);
        }
    }
#endif

    private bool HasAnyInputKind(GameInputKind inputKind)
    {
        return (InputKind & inputKind) != 0;
    }
}
