using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// GameInput 低階讀取資料包裝。
/// </summary>
public sealed class GameInputReading : IDisposable
{
    private IGameInputReading? _native;
    private bool _disposed;
    private GameInputKind? _cachedInputKind;
    private ulong? _cachedTimestamp;

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
    /// <remarks>
    /// 同一筆讀取資料的輸入種類在其存續期間為固定值，此屬性會在同一個 <see cref="GameInputReading"/>
    /// 執行個體上快取結果，避免重複的原生呼叫。
    /// </remarks>
    public GameInputKind InputKind
    {
        get
        {
            return _cachedInputKind ??= Native.GetInputKind();
        }
    }

    /// <summary>
    /// 讀取資料的 GameInput 時間戳記。
    /// </summary>
    /// <remarks>
    /// 同一筆讀取資料的時間戳記在其存續期間為固定值，此屬性會在同一個 <see cref="GameInputReading"/>
    /// 執行個體上快取結果，避免重複的原生呼叫。
    /// </remarks>
    public ulong Timestamp
    {
        get
        {
            return _cachedTimestamp ??= Native.GetTimestamp();
        }
    }

    /// <summary>
    /// 取得 reading 所屬裝置。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputDevice? GetDevice()
    {
        Native.GetDevice(out IGameInputDevice? device);
        return device is null ? null : new GameInputDevice(device);
    }

    /// <summary>
    /// 取得 controller 軸狀態。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public float[] GetControllerAxisState()
    {
        uint count = Native.GetControllerAxisCount();
        if (count == 0)
        {
            return [];
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
    /// 使用既有陣列緩衝區取得 controller 軸狀態，避免每影格分配。
    /// </summary>
    /// <param name="stateArray">接收狀態資料的 managed 陣列。</param>
    /// <returns>原生 API 寫入的元素數。</returns>
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
        return Native.GetControllerAxisState((uint)stateArray.Length, stateArray);
    }

    /// <summary>
    /// 取得 controller 按鈕狀態。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool[] GetControllerButtonState()
    {
        uint count = Native.GetControllerButtonCount();
        if (count == 0)
        {
            return [];
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
    /// 使用既有 byte 陣列緩衝區取得 controller 按鈕狀態，避免每影格分配。
    /// </summary>
    /// <param name="stateArray">接收狀態資料的 managed byte 陣列。</param>
    /// <returns>原生 API 寫入的元素數。</returns>
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
        return Native.GetControllerButtonState((uint)stateArray.Length, stateArray);
    }

    /// <summary>
    /// 取得 controller switch 狀態。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputSwitchPosition[] GetControllerSwitchState()
    {
        uint count = Native.GetControllerSwitchCount();
        if (count == 0)
        {
            return [];
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
    /// 使用既有陣列緩衝區取得 controller switch 狀態，避免每影格分配。
    /// </summary>
    /// <param name="stateArray">接收狀態資料的 managed 陣列。</param>
    /// <returns>原生 API 寫入的元素數。</returns>
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
        return Native.GetControllerSwitchState((uint)stateArray.Length, stateArray);
    }

    /// <summary>
    /// 取得鍵盤按鍵狀態。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputKeyState[] GetKeyState()
    {
        uint count = Native.GetKeyCount();
        if (count == 0)
        {
            return [];
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
    /// 使用既有陣列緩衝區取得鍵盤按鍵狀態，避免每影格分配。
    /// </summary>
    /// <param name="stateArray">接收狀態資料的 managed 陣列。</param>
    /// <returns>原生 API 寫入的元素數。</returns>
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
        return Native.GetKeyState((uint)stateArray.Length, stateArray);
    }

    /// <summary>
    /// 嘗試讀取 gamepad 狀態。
    /// </summary>
    /// <param name="state">接收狀態資料的輸出欄位。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool TryGetGamepadState(out GameInputGamepadState state)
    {
        return Native.GetGamepadState(out state);
    }

    /// <summary>
    /// 嘗試建立不持有原生生命週期的 gamepad 快照。
    /// </summary>
    /// <param name="snapshot">成功時接收 gamepad 快照。</param>
    /// <returns>若 reading 包含 gamepad 狀態，傳回 true；否則傳回 false。</returns>
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
    /// 嘗試讀取滑鼠狀態。
    /// </summary>
    /// <param name="state">接收狀態資料的輸出欄位。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool TryGetMouseState(out GameInputMouseState state)
    {
        return Native.GetMouseState(out state);
    }

    /// <summary>
    /// 嘗試建立不持有原生生命週期的滑鼠快照。
    /// </summary>
    /// <param name="snapshot">成功時接收滑鼠快照。</param>
    /// <returns>若 reading 包含滑鼠狀態，傳回 true；否則傳回 false。</returns>
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
    /// 嘗試讀取感測器狀態。
    /// </summary>
    /// <param name="state">接收狀態資料的輸出欄位。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool TryGetSensorsState(out GameInputSensorsState state)
    {
        return Native.GetSensorsState(out state);
    }

    /// <summary>
    /// 嘗試建立不持有原生生命週期的感測器快照。
    /// </summary>
    /// <param name="snapshot">成功時接收感測器快照。</param>
    /// <returns>若 reading 包含感測器狀態，傳回 true；否則傳回 false。</returns>
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
    /// 嘗試讀取 arcade stick 狀態。
    /// </summary>
    /// <param name="state">接收狀態資料的輸出欄位。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool TryGetArcadeStickState(out GameInputArcadeStickState state)
    {
        return Native.GetArcadeStickState(out state);
    }

    /// <summary>
    /// 嘗試建立不持有原生生命週期的 arcade stick 快照。
    /// </summary>
    /// <param name="snapshot">成功時接收 arcade stick 快照。</param>
    /// <returns>若 reading 包含 arcade stick 狀態，傳回 true；否則傳回 false。</returns>
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
    /// 嘗試讀取 flight stick 狀態。
    /// </summary>
    /// <param name="state">接收狀態資料的輸出欄位。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool TryGetFlightStickState(out GameInputFlightStickState state)
    {
        return Native.GetFlightStickState(out state);
    }

    /// <summary>
    /// 嘗試建立不持有原生生命週期的 flight stick 快照。
    /// </summary>
    /// <param name="snapshot">成功時接收 flight stick 快照。</param>
    /// <returns>若 reading 包含 flight stick 狀態，傳回 true；否則傳回 false。</returns>
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
    /// 嘗試讀取 racing wheel 狀態。
    /// </summary>
    /// <param name="state">接收狀態資料的輸出欄位。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool TryGetRacingWheelState(out GameInputRacingWheelState state)
    {
        return Native.GetRacingWheelState(out state);
    }

    /// <summary>
    /// 嘗試建立不持有原生生命週期的 racing wheel 快照。
    /// </summary>
    /// <param name="snapshot">成功時接收 racing wheel 快照。</param>
    /// <returns>若 reading 包含 racing wheel 狀態，傳回 true；否則傳回 false。</returns>
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
    /// 嘗試取得 raw report。
    /// </summary>
    /// <param name="report">要傳送或操作的 raw device report。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
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

    /// <summary>
    /// 嘗試建立不持有原生生命週期的 keyboard 快照。
    /// </summary>
    /// <param name="snapshot">成功時接收 keyboard 快照。</param>
    /// <returns>若 reading 包含 keyboard 狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetKeyboardSnapshot(out KeyboardReadingSnapshot? snapshot)
    {
        if (!HasAnyInputKind(GameInputKind.GameInputKindKeyboard))
        {
            snapshot = null;
            return false;
        }

        snapshot = new KeyboardReadingSnapshot(Timestamp, GetKeyState());
        return true;
    }

    /// <summary>
    /// 嘗試建立不持有原生生命週期的 controller 快照。
    /// </summary>
    /// <param name="snapshot">成功時接收 controller 快照。</param>
    /// <returns>若 reading 包含 controller 狀態，傳回 true；否則傳回 false。</returns>
    public bool TryGetControllerSnapshot(out ControllerReadingSnapshot? snapshot)
    {
        if (!HasAnyInputKind(GameInputKind.GameInputKindController))
        {
            snapshot = null;
            return false;
        }

        snapshot = new ControllerReadingSnapshot(
            Timestamp,
            GetControllerAxisState(),
            GetControllerButtonState(),
            GetControllerSwitchState());
        return true;
    }

    /// <summary>
    /// 嘗試建立不持有原生 raw report 生命週期的快照。
    /// </summary>
    /// <param name="snapshot">成功時接收 raw report 快照。</param>
    /// <returns>若 reading 包含 raw report，傳回 true；否則傳回 false。</returns>
    public bool TryGetRawReportSnapshot(out RawDeviceReportSnapshot? snapshot)
    {
        if (!TryGetRawReport(out GameInputRawDeviceReport? report))
        {
            snapshot = null;
            return false;
        }

        using (report)
        {
            snapshot = new RawDeviceReportSnapshot(Timestamp, report!.GetReportInfo(), report.GetRawData());
            return true;
        }
    }

    /// <summary>
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
            if (Marshal.IsComObject(_native))
            {
                _ = Marshal.ReleaseComObject(_native);
            }
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

    private bool HasAnyInputKind(GameInputKind inputKind)
    {
        return (InputKind & inputKind) != 0;
    }
}
