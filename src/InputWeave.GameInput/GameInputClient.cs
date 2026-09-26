using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// The high-level C# entry point for GameInput.
/// GameInput 的高階 C# 入口點。
/// </summary>
public sealed class GameInputClient : IDisposable
{
    /// <summary>
    /// Raised when an uncaught exception occurs while a native callback runs a user delegate; the exception is not rethrown to the native caller.
    /// 當原生回呼執行使用者委派時發生未攔截例外時觸發；例外不會繼續拋出至原生呼叫端。
    /// </summary>
    public static event EventHandler<GameInputCallbackExceptionEventArgs>? UnhandledCallbackException;

    /// <summary>
    /// The maximum platform string length, in characters, accepted by <see cref="FindDeviceFromPlatformString"/>.
    /// <see cref="FindDeviceFromPlatformString"/> 接受的平台字串長度上限（字元數）。
    /// </summary>
    /// <remarks>
    /// Actual platform device strings (such as device paths) are usually far shorter than this limit; exceeding it is treated as caller misuse and rejected outright,
    /// preventing abnormally long strings from being passed into the native call.
    /// 實際平台裝置字串（例如裝置路徑）通常遠短於這個上限；超過時視為呼叫端誤用，直接拒絕，
    /// 避免把異常長的字串傳入原生呼叫。
    /// </remarks>
    public const int MaxPlatformStringLength = 1024;

#if NET8_0_OR_GREATER
    private static unsafe IntPtr ReadingCallbackPointer => (IntPtr)(delegate* unmanaged[Stdcall]<ulong, IntPtr, IGameInputReading, void>)&OnReadingCallback;

    private static unsafe IntPtr DeviceCallbackPointer => (IntPtr)(delegate* unmanaged[Stdcall]<ulong, IntPtr, IGameInputDevice, ulong, GameInputDeviceStatus, GameInputDeviceStatus, void>)&OnDeviceCallback;

    private static unsafe IntPtr SystemButtonCallbackPointer => (IntPtr)(delegate* unmanaged[Stdcall]<ulong, IntPtr, IGameInputDevice, ulong, GameInputSystemButtons, GameInputSystemButtons, void>)&OnSystemButtonCallback;

    private static unsafe IntPtr KeyboardLayoutCallbackPointer => (IntPtr)(delegate* unmanaged[Stdcall]<ulong, IntPtr, IGameInputDevice, ulong, uint, uint, void>)&OnKeyboardLayoutCallback;
#else
    // .NET Framework 沒有 UnmanagedCallersOnly；依「Marshalling a Delegate as a Callback Method」的做法，
    // 以 UnmanagedFunctionPointer 委派取得原生可呼叫的函式指標，並用靜態欄位讓委派在整個處理序期間保持存活，
    // 避免原生端呼叫到已被 GC 回收的 thunk。
    private static readonly GameInputReadingCallback s_readingCallback = OnReadingCallback;
    private static readonly GameInputDeviceCallback s_deviceCallback = OnDeviceCallback;
    private static readonly GameInputSystemButtonCallback s_systemButtonCallback = OnSystemButtonCallback;
    private static readonly GameInputKeyboardLayoutCallback s_keyboardLayoutCallback = OnKeyboardLayoutCallback;

    private static IntPtr ReadingCallbackPointer { get; } = Marshal.GetFunctionPointerForDelegate(s_readingCallback);

    private static IntPtr DeviceCallbackPointer { get; } = Marshal.GetFunctionPointerForDelegate(s_deviceCallback);

    private static IntPtr SystemButtonCallbackPointer { get; } = Marshal.GetFunctionPointerForDelegate(s_systemButtonCallback);

    private static IntPtr KeyboardLayoutCallbackPointer { get; } = Marshal.GetFunctionPointerForDelegate(s_keyboardLayoutCallback);
#endif

#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _syncRoot = new();
#else
    private readonly object _syncRoot = new();
#endif
    private static IntPtr s_anchoredRoot;
    private readonly List<GameInputCallbackRegistration> _registrations = [];
    private readonly List<Action> _pendingWaitCancellations = [];
    private readonly GameInputComHandle _handle;
    private int _disposeState;

    internal GameInputClient(GameInputComHandle handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Creates a GameInput v3 client.
    /// 建立 GameInput v3 用戶端。
    /// </summary>
    /// <remarks>
    /// All target frameworks call GameInput through raw vtable function pointers instead of COM Interop runtime callable wrappers,
    /// so the client and its child objects can be used from any thread regardless of COM apartment, matching GameInput's own
    /// thread-safe design.
    /// 所有目標框架都透過原始 vtable 函式指標呼叫 GameInput，而非 COM Interop 的執行階段可呼叫包裝（RCW），
    /// 因此用戶端與其子物件可以在任何執行緒上使用，不受 COM apartment 限制，與 GameInput 本身的執行緒安全設計一致。
    /// </remarks>
    /// <exception cref="GameInputException">GameInput initialization failed. GameInput 初始化失敗。</exception>
    /// <returns>The newly created <see cref="GameInputClient"/> instance. 新建立的 <see cref="GameInputClient"/> 執行個體。</returns>
    public static GameInputClient Create()
    {
        Guid iid = GameInputIids.IGameInput;
        int hResult = GameInputNativeMethods.GameInputInitialize(ref iid, out IntPtr nativePointer);
        GameInputException.ThrowIfFailed(hResult);

        // GameInputInitialize 的輸出指標依 COM 慣例已 AddRef，擁有權直接轉交給 SafeHandle。
        GameInputComHandle handle = new(nativePointer);
        AnchorRuntimeRoot(nativePointer);
        try
        {
            return new GameInputClient(handle);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Gets the current GameInput timestamp.
    /// 取得 GameInput 目前時間戳記。
    /// </summary>
    /// <returns>The current GameInput timestamp. 目前的 GameInput 時間戳記。</returns>
    public ulong GetCurrentTimestamp()
    {
        using ComLease<IGameInput> call = EnterNative();
        return call.Native.GetCurrentTimestamp();
    }

    /// <summary>
    /// Sets the GameInput focus policy.
    /// 設定 GameInput 焦點政策。
    /// </summary>
    /// <param name="policy">The focus policy to apply. 要套用的焦點政策。</param>
    public void SetFocusPolicy(GameInputFocusPolicy policy)
    {
        using ComLease<IGameInput> call = EnterNative();
        call.Native.SetFocusPolicy(policy);
    }

    /// <summary>
    /// Gets the current gamepad snapshot.
    /// 取得目前 gamepad 快照。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The current gamepad snapshot, or null when no reading is available. 目前的 gamepad 快照；沒有可用的讀取資料時為 null。</returns>
    public GamepadReadingSnapshot? GetCurrentGamepad(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindGamepad,
            device,
            static (GameInputReading reading, out GamepadReadingSnapshot snapshot) => reading.TryGetGamepadSnapshot(out snapshot));
    }

    /// <summary>
    /// Gets the current keyboard snapshot.
    /// 取得目前 keyboard 快照。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The current keyboard snapshot, or null when no reading is available. 目前的 keyboard 快照；沒有可用的讀取資料時為 null。</returns>
    public KeyboardReadingSnapshot? GetCurrentKeyboard(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindKeyboard,
            device,
            static (GameInputReading reading, out KeyboardReadingSnapshot snapshot) => reading.TryGetKeyboardSnapshot(out snapshot));
    }

    /// <summary>
    /// Gets the current mouse snapshot.
    /// 取得目前 mouse 快照。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The current mouse snapshot, or null when no reading is available. 目前的 mouse 快照；沒有可用的讀取資料時為 null。</returns>
    public MouseReadingSnapshot? GetCurrentMouse(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindMouse,
            device,
            static (GameInputReading reading, out MouseReadingSnapshot snapshot) => reading.TryGetMouseSnapshot(out snapshot));
    }

    /// <summary>
    /// Gets the current sensors snapshot.
    /// 取得目前 sensors 快照。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The current sensors snapshot, or null when no reading is available. 目前的 sensors 快照；沒有可用的讀取資料時為 null。</returns>
    public SensorsReadingSnapshot? GetCurrentSensors(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindSensors,
            device,
            static (GameInputReading reading, out SensorsReadingSnapshot snapshot) => reading.TryGetSensorsSnapshot(out snapshot));
    }

    /// <summary>
    /// Gets the current controller snapshot.
    /// 取得目前一般 controller 快照。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The current controller snapshot, or null when no reading is available. 目前的一般 controller 快照；沒有可用的讀取資料時為 null。</returns>
    public ControllerReadingSnapshot? GetCurrentController(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindController,
            device,
            static (GameInputReading reading, out ControllerReadingSnapshot snapshot) => reading.TryGetControllerSnapshot(out snapshot));
    }

    /// <summary>
    /// Gets the current arcade stick snapshot.
    /// 取得目前 arcade stick 快照。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The current arcade stick snapshot, or null when no reading is available. 目前的 arcade stick 快照；沒有可用的讀取資料時為 null。</returns>
    public ArcadeStickReadingSnapshot? GetCurrentArcadeStick(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindArcadeStick,
            device,
            static (GameInputReading reading, out ArcadeStickReadingSnapshot snapshot) => reading.TryGetArcadeStickSnapshot(out snapshot));
    }

    /// <summary>
    /// Gets the current flight stick snapshot.
    /// 取得目前 flight stick 快照。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The current flight stick snapshot, or null when no reading is available. 目前的 flight stick 快照；沒有可用的讀取資料時為 null。</returns>
    public FlightStickReadingSnapshot? GetCurrentFlightStick(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindFlightStick,
            device,
            static (GameInputReading reading, out FlightStickReadingSnapshot snapshot) => reading.TryGetFlightStickSnapshot(out snapshot));
    }

    /// <summary>
    /// Gets the current racing wheel snapshot.
    /// 取得目前 racing wheel 快照。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The current racing wheel snapshot, or null when no reading is available. 目前的 racing wheel 快照；沒有可用的讀取資料時為 null。</returns>
    public RacingWheelReadingSnapshot? GetCurrentRacingWheel(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindRacingWheel,
            device,
            static (GameInputReading reading, out RacingWheelReadingSnapshot snapshot) => reading.TryGetRacingWheelSnapshot(out snapshot));
    }

    /// <summary>
    /// Gets the current raw device report snapshot.
    /// 取得目前 raw device report 快照。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The current raw device report snapshot, or null when no reading is available. 目前的 raw device report 快照；沒有可用的讀取資料時為 null。</returns>
    public RawDeviceReportSnapshot? GetCurrentRawReport(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindRawDeviceReport,
            device,
            static (GameInputReading reading, out RawDeviceReportSnapshot snapshot) => reading.TryGetRawReportSnapshot(out snapshot));
    }

    /// <summary>
    /// Tries to get the current gamepad snapshot from any device, without having to unwrap a nullable result.
    /// 嘗試從任一裝置取得目前 gamepad 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="snapshot">Receives the current gamepad snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 gamepad 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentGamepad(out GamepadReadingSnapshot snapshot)
    {
        return TryGetCurrentGamepad(null, out snapshot);
    }

    /// <summary>
    /// Tries to get the current gamepad snapshot, without having to unwrap a nullable result.
    /// 嘗試取得目前 gamepad 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="snapshot">Receives the current gamepad snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 gamepad 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentGamepad(GameInputDevice? device, out GamepadReadingSnapshot snapshot)
    {
        return TryGetValue(GetCurrentGamepad(device), out snapshot);
    }

    /// <summary>
    /// Tries to get the current keyboard snapshot from any device, without having to unwrap a nullable result.
    /// 嘗試從任一裝置取得目前 keyboard 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="snapshot">Receives the current keyboard snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 keyboard 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentKeyboard(out KeyboardReadingSnapshot snapshot)
    {
        return TryGetCurrentKeyboard(null, out snapshot);
    }

    /// <summary>
    /// Tries to get the current keyboard snapshot, without having to unwrap a nullable result.
    /// 嘗試取得目前 keyboard 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="snapshot">Receives the current keyboard snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 keyboard 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentKeyboard(GameInputDevice? device, out KeyboardReadingSnapshot snapshot)
    {
        return TryGetValue(GetCurrentKeyboard(device), out snapshot);
    }

    /// <summary>
    /// Tries to get the current mouse snapshot from any device, without having to unwrap a nullable result.
    /// 嘗試從任一裝置取得目前 mouse 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="snapshot">Receives the current mouse snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 mouse 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentMouse(out MouseReadingSnapshot snapshot)
    {
        return TryGetCurrentMouse(null, out snapshot);
    }

    /// <summary>
    /// Tries to get the current mouse snapshot, without having to unwrap a nullable result.
    /// 嘗試取得目前 mouse 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="snapshot">Receives the current mouse snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 mouse 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentMouse(GameInputDevice? device, out MouseReadingSnapshot snapshot)
    {
        return TryGetValue(GetCurrentMouse(device), out snapshot);
    }

    /// <summary>
    /// Tries to get the current sensors snapshot from any device, without having to unwrap a nullable result.
    /// 嘗試從任一裝置取得目前 sensors 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="snapshot">Receives the current sensors snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 sensors 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentSensors(out SensorsReadingSnapshot snapshot)
    {
        return TryGetCurrentSensors(null, out snapshot);
    }

    /// <summary>
    /// Tries to get the current sensors snapshot, without having to unwrap a nullable result.
    /// 嘗試取得目前 sensors 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="snapshot">Receives the current sensors snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 sensors 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentSensors(GameInputDevice? device, out SensorsReadingSnapshot snapshot)
    {
        return TryGetValue(GetCurrentSensors(device), out snapshot);
    }

    /// <summary>
    /// Tries to get the current controller snapshot from any device, without having to unwrap a nullable result.
    /// 嘗試從任一裝置取得目前 一般 controller 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="snapshot">Receives the current controller snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 一般 controller 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentController(out ControllerReadingSnapshot snapshot)
    {
        return TryGetCurrentController(null, out snapshot);
    }

    /// <summary>
    /// Tries to get the current controller snapshot, without having to unwrap a nullable result.
    /// 嘗試取得目前 一般 controller 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="snapshot">Receives the current controller snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 一般 controller 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentController(GameInputDevice? device, out ControllerReadingSnapshot snapshot)
    {
        return TryGetValue(GetCurrentController(device), out snapshot);
    }

    /// <summary>
    /// Tries to get the current arcade stick snapshot from any device, without having to unwrap a nullable result.
    /// 嘗試從任一裝置取得目前 arcade stick 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="snapshot">Receives the current arcade stick snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 arcade stick 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentArcadeStick(out ArcadeStickReadingSnapshot snapshot)
    {
        return TryGetCurrentArcadeStick(null, out snapshot);
    }

    /// <summary>
    /// Tries to get the current arcade stick snapshot, without having to unwrap a nullable result.
    /// 嘗試取得目前 arcade stick 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="snapshot">Receives the current arcade stick snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 arcade stick 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentArcadeStick(GameInputDevice? device, out ArcadeStickReadingSnapshot snapshot)
    {
        return TryGetValue(GetCurrentArcadeStick(device), out snapshot);
    }

    /// <summary>
    /// Tries to get the current flight stick snapshot from any device, without having to unwrap a nullable result.
    /// 嘗試從任一裝置取得目前 flight stick 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="snapshot">Receives the current flight stick snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 flight stick 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentFlightStick(out FlightStickReadingSnapshot snapshot)
    {
        return TryGetCurrentFlightStick(null, out snapshot);
    }

    /// <summary>
    /// Tries to get the current flight stick snapshot, without having to unwrap a nullable result.
    /// 嘗試取得目前 flight stick 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="snapshot">Receives the current flight stick snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 flight stick 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentFlightStick(GameInputDevice? device, out FlightStickReadingSnapshot snapshot)
    {
        return TryGetValue(GetCurrentFlightStick(device), out snapshot);
    }

    /// <summary>
    /// Tries to get the current racing wheel snapshot from any device, without having to unwrap a nullable result.
    /// 嘗試從任一裝置取得目前 racing wheel 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="snapshot">Receives the current racing wheel snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 racing wheel 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentRacingWheel(out RacingWheelReadingSnapshot snapshot)
    {
        return TryGetCurrentRacingWheel(null, out snapshot);
    }

    /// <summary>
    /// Tries to get the current racing wheel snapshot, without having to unwrap a nullable result.
    /// 嘗試取得目前 racing wheel 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="snapshot">Receives the current racing wheel snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 racing wheel 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentRacingWheel(GameInputDevice? device, out RacingWheelReadingSnapshot snapshot)
    {
        return TryGetValue(GetCurrentRacingWheel(device), out snapshot);
    }

    /// <summary>
    /// Tries to get the current raw device report snapshot from any device, without having to unwrap a nullable result.
    /// 嘗試從任一裝置取得目前 raw device report 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="snapshot">Receives the current raw device report snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 raw device report 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentRawReport(out RawDeviceReportSnapshot snapshot)
    {
        return TryGetCurrentRawReport(null, out snapshot);
    }

    /// <summary>
    /// Tries to get the current raw device report snapshot, without having to unwrap a nullable result.
    /// 嘗試取得目前 raw device report 快照，不需要再展開可為 null 的結果。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="snapshot">Receives the current raw device report snapshot when one is available; otherwise the default value. 有可用資料時接收目前的 raw device report 快照；否則為預設值。</param>
    /// <returns>Returns true when a reading is available; otherwise returns false. 有可用的讀取資料時傳回 true；否則傳回 false。</returns>
    public bool TryGetCurrentRawReport(GameInputDevice? device, out RawDeviceReportSnapshot snapshot)
    {
        return TryGetValue(GetCurrentRawReport(device), out snapshot);
    }

    /// <summary>
    /// Gets the current low-level reading of the specified kind.
    /// 取得目前指定種類的低階讀取資料。
    /// </summary>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The current low-level reading of the specified kind, or null when none is available. 目前指定種類的低階讀取資料；沒有可用資料時為 null。</returns>
    public GameInputReading? GetCurrentReading(GameInputKind inputKind, GameInputDevice? device = null)
    {
        return GetCurrentReadingCore(inputKind, device, scoped: false);
    }

    private GameInputReading? GetCurrentReadingCore(GameInputKind inputKind, GameInputDevice? device, bool scoped)
    {
        using ComLease<IGameInput> call = EnterNative();
        using ComLease<IGameInputDevice> deviceLease = device is null ? default : device.EnterNative();
        int hResult = call.Native.GetCurrentReading(inputKind, deviceLease.NativeOrNull, out IGameInputReading? nativeReading);
        if (hResult == GameInputHResult.ReadingNotFound || hResult == GameInputHResult.InputKindNotPresent)
        {
            return null;
        }

        GameInputException.ThrowIfFailed(hResult);
        if (nativeReading is not { } reading)
        {
            return null;
        }

        return scoped ? GameInputReading.CreateScoped(reading) : new GameInputReading(reading);
    }

    /// <summary>
    /// Gets the reading after the specified reference reading.
    /// 取得指定參考 reading 之後的 reading。
    /// </summary>
    /// <param name="referenceReading">The reading used as the reference for the query. 作為查詢基準的 reading。</param>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The reading after the reference reading, or null when none exists. 參考 reading 之後的 reading；不存在時為 null。</returns>
    public GameInputReading? GetNextReading(GameInputReading referenceReading, GameInputKind inputKind, GameInputDevice? device = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(referenceReading);
#else
        if (referenceReading is null)
        {
            throw new ArgumentNullException(nameof(referenceReading));
        }
#endif

        using ComLease<IGameInput> call = EnterNative();
        using ComLease<IGameInputReading> referenceLease = referenceReading.EnterNative();
        using ComLease<IGameInputDevice> deviceLease = device is null ? default : device.EnterNative();
        int hResult = call.Native.GetNextReading(referenceLease.Native, inputKind, deviceLease.NativeOrNull, out IGameInputReading? nativeReading);
        if (hResult == GameInputHResult.ReadingNotFound || hResult == GameInputHResult.InputKindNotPresent)
        {
            return null;
        }

        GameInputException.ThrowIfFailed(hResult);
        return nativeReading is { } reading ? new GameInputReading(reading) : null;
    }

    /// <summary>
    /// Gets the reading before the specified reference reading.
    /// 取得指定參考 reading 之前的 reading。
    /// </summary>
    /// <param name="referenceReading">The reading used as the reference for the query. 作為查詢基準的 reading。</param>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <returns>The reading before the reference reading, or null when none exists. 參考 reading 之前的 reading；不存在時為 null。</returns>
    public GameInputReading? GetPreviousReading(GameInputReading referenceReading, GameInputKind inputKind, GameInputDevice? device = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(referenceReading);
#else
        if (referenceReading is null)
        {
            throw new ArgumentNullException(nameof(referenceReading));
        }
#endif

        using ComLease<IGameInput> call = EnterNative();
        using ComLease<IGameInputReading> referenceLease = referenceReading.EnterNative();
        using ComLease<IGameInputDevice> deviceLease = device is null ? default : device.EnterNative();
        int hResult = call.Native.GetPreviousReading(referenceLease.Native, inputKind, deviceLease.NativeOrNull, out IGameInputReading? nativeReading);
        if (hResult == GameInputHResult.ReadingNotFound || hResult == GameInputHResult.InputKindNotPresent)
        {
            return null;
        }

        GameInputException.ThrowIfFailed(hResult);
        return nativeReading is { } reading ? new GameInputReading(reading) : null;
    }

    /// <summary>
    /// Creates a GameInput dispatcher.
    /// 建立 GameInput dispatcher。
    /// </summary>
    /// <returns>The newly created GameInput dispatcher. 新建立的 GameInput dispatcher。</returns>
    public GameInputDispatcher CreateDispatcher()
    {
        using ComLease<IGameInput> call = EnterNative();
        int hResult = call.Native.CreateDispatcher(out IGameInputDispatcher? dispatcher);
        GameInputException.ThrowIfFailed(hResult);
        return dispatcher is { } dispatcherValue
            ? new GameInputDispatcher(dispatcherValue)
            : throw new GameInputException(GameInputHResult.ObjectNoLongerExists);
    }

    /// <summary>
    /// Finds a device by its device ID.
    /// 依裝置 ID 尋找裝置。
    /// </summary>
    /// <param name="deviceId">The GameInput device identifier. GameInput 裝置識別值。</param>
    /// <returns>The matching device wrapper. 符合的裝置包裝。</returns>
    public GameInputDevice FindDeviceFromId(in AppLocalDeviceId deviceId)
    {
        // 原生 API 的參數是唯讀（const APP_LOCAL_DEVICE_ID*），高階簽章用 in 符合 C# 慣用法；
        // 產生式互通層簽章為 ref，複製到區域變數轉交。
        AppLocalDeviceId local = deviceId;
        using ComLease<IGameInput> call = EnterNative();
        int hResult = call.Native.FindDeviceFromId(ref local, out IGameInputDevice? device);
        GameInputException.ThrowIfFailed(hResult);
        return device is { } deviceValue
            ? new GameInputDevice(deviceValue)
            : throw new GameInputException(GameInputHResult.DeviceNotFound);
    }

    /// <summary>
    /// Finds a device by its platform string.
    /// 依平台字串尋找裝置。
    /// </summary>
    /// <param name="value">The value to pass in. 要傳入的值。</param>
    /// <returns>The matching device wrapper. 符合的裝置包裝。</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is blank, or its length exceeds <see cref="MaxPlatformStringLength"/>. <paramref name="value"/> 為空白，或長度超過 <see cref="MaxPlatformStringLength"/>。</exception>
    public GameInputDevice FindDeviceFromPlatformString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("平台字串不可為空白。", nameof(value));
        }

        if (value.Length > MaxPlatformStringLength)
        {
            throw new ArgumentException($"平台字串長度（{value.Length}）超過上限（{MaxPlatformStringLength}）。", nameof(value));
        }

        using ComLease<IGameInput> call = EnterNative();
        int hResult = call.Native.FindDeviceFromPlatformString(value, out IGameInputDevice? device);
        GameInputException.ThrowIfFailed(hResult);
        return device is { } deviceValue
            ? new GameInputDevice(deviceValue)
            : throw new GameInputException(GameInputHResult.DeviceNotFound);
    }

    /// <summary>
    /// Enumerates the devices currently matching the criteria.
    /// 列舉目前符合條件的裝置。
    /// </summary>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="statusFilter">The device status to filter. 要篩選的裝置狀態。</param>
    /// <returns>The list of devices matching the requested input kind and status filter. 符合指定輸入種類與狀態篩選的裝置清單。</returns>
    public IReadOnlyList<GameInputDevice> EnumerateDevices(GameInputKind inputKind, GameInputDeviceStatus statusFilter = GameInputDeviceStatus.GameInputDeviceConnected)
    {
        // 租約涵蓋註冊到 finally 的停止與解除註冊，避免 Dispose 在列舉期間釋放原生物件。
        using ComLease<IGameInput> call = EnterNative();
        DeviceEnumerationContext context = new();
        GCHandle contextHandle = GCHandle.Alloc(context);
        ulong token = 0;
        try
        {
            int hResult = call.Native.RegisterDeviceCallback(
                device: null,
                inputKind,
                statusFilter,
                GameInputEnumerationKind.GameInputBlockingEnumeration,
                GCHandle.ToIntPtr(contextHandle),
                DeviceCallbackPointer,
                out token);

            GameInputException.ThrowIfFailed(hResult);
            return context.Detach();
        }
        catch
        {
            // 列舉失敗時，回呼可能已收集部分裝置；這些包裝各自持有 COM 參考，不能等終結器才釋放。
            context.Deactivate();
            foreach (GameInputDevice device in context.Detach())
            {
                device.Dispose();
            }

            throw;
        }
        finally
        {
            context.Deactivate();

            // 先分離裝置清單：萬一 UnregisterCallback 傳回 false 而必須保留 GCHandle，保留的內容也不會讓回傳的裝置包裝
            // 無法被終結而洩漏 COM 參考。
            context.Detach();
            bool unregistered = true;
            if (token != 0)
            {
                // 不要先呼叫 StopCallback：它會在背景非同步移除註冊，移除完成後 UnregisterCallback 找不到 token 而傳回 false
                // （實機量測：先 Stop 約 5% 為 false，Stop 後等 50ms 則 100%；只呼叫 Unregister 為 0%）。
                // UnregisterCallback 本身就保證不再派送，並會等待進行中的回呼結束。
                unregistered = call.Native.UnregisterCallback(token);
            }

            // 與 GameInputCallbackRegistration 相同：UnregisterCallback 成功返回前不得釋放回呼資源。
            if (unregistered && contextHandle.IsAllocated)
            {
                contextHandle.Free();
            }
        }
    }

    /// <summary>
    /// Enumerates the matching devices asynchronously on a background thread, preventing the calling thread from being blocked by
    /// the native blocking enumeration.
    /// 以背景執行緒非同步列舉目前符合條件的裝置，避免呼叫端執行緒被原生阻塞式列舉卡住。
    /// </summary>
    /// <remarks>
    /// This method merely wraps <see cref="EnumerateDevices(GameInputKind, GameInputDeviceStatus)"/> in
    /// <see cref="Task.Run(Action)"/>; it is not an asynchronous method that waits for native events.
    /// 這個方法只是把 <see cref="EnumerateDevices(GameInputKind, GameInputDeviceStatus)"/> 包在
    /// <see cref="Task.Run(Action)"/> 中執行，不是等待原生事件的非同步方法。
    /// </remarks>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="statusFilter">The device status to filter. 要篩選的裝置狀態。</param>
    /// <param name="cancellationToken">The cancellation token. 取消語彙。</param>
    /// <returns>A task that produces the list of matching devices. 產生符合條件裝置清單的工作。</returns>
    public Task<IReadOnlyList<GameInputDevice>> EnumerateDevicesAsync(
        GameInputKind inputKind,
        GameInputDeviceStatus statusFilter = GameInputDeviceStatus.GameInputDeviceConnected,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => EnumerateDevices(inputKind, statusFilter), cancellationToken);
    }

    /// <summary>
    /// Creates an aggregate device.
    /// 建立聚合裝置。
    /// </summary>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <returns>The device identifier of the aggregate device. 聚合裝置的裝置識別值。</returns>
    public AppLocalDeviceId CreateAggregateDevice(GameInputKind inputKind)
    {
        using ComLease<IGameInput> call = EnterNative();
        int hResult = call.Native.CreateAggregateDevice(inputKind, out AppLocalDeviceId deviceId);
        GameInputException.ThrowIfFailed(hResult);
        return deviceId;
    }

    /// <summary>
    /// Disables an aggregate device.
    /// 停用聚合裝置。
    /// </summary>
    /// <param name="deviceId">The GameInput device identifier. GameInput 裝置識別值。</param>
    public void DisableAggregateDevice(in AppLocalDeviceId deviceId)
    {
        // 原生 API 的參數是唯讀（const APP_LOCAL_DEVICE_ID*），高階簽章用 in 符合 C# 慣用法；
        // 產生式互通層簽章為 ref，複製到區域變數轉交。
        AppLocalDeviceId local = deviceId;
        using ComLease<IGameInput> call = EnterNative();
        int hResult = call.Native.DisableAggregateDevice(ref local);
        GameInputException.ThrowIfFailed(hResult);
    }

    /// <summary>
    /// Registers a reading callback.
    /// 註冊 reading callback。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="handler">The managed callback handler to register. 要註冊的 managed callback handler。</param>
    /// <returns>The callback registration used to unregister the callback. 用來解除註冊的 callback 註冊。</returns>
    public GameInputCallbackRegistration RegisterReadingCallback(GameInputDevice? device, GameInputKind inputKind, GameInputReadingHandler handler)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(handler);
#else
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
#endif

        ReadingCallbackContext context = new(handler);
        GCHandle handle = GCHandle.Alloc(context);
        ulong token = 0;
        try
        {
            using ComLease<IGameInput> call = EnterNative();
            using ComLease<IGameInputDevice> deviceLease = device is null ? default : device.EnterNative();
            int hResult = call.Native.RegisterReadingCallback(
                deviceLease.NativeOrNull,
                inputKind,
                GCHandle.ToIntPtr(handle),
                ReadingCallbackPointer,
                out token);
            GameInputException.ThrowIfFailed(hResult);
            return AddRegistration(token, handle, context.Deactivate);
        }
        catch
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }

            throw;
        }
    }

    /// <summary>
    /// Asynchronously waits for the next matching reading and converts it into a safe result with <paramref name="selector"/>
    /// while the native callback is still valid.
    /// 非同步等待下一筆符合條件的 reading，並在原生回呼仍然有效期間內以 <paramref name="selector"/> 轉換為安全結果。
    /// </summary>
    /// <remarks>
    /// The <see cref="GameInputReading"/> received by <paramref name="selector"/> is valid only while the callback executes; do
    /// not return the reading itself, or any reference tied to its native lifetime, from <paramref name="selector"/> — convert it
    /// into a snapshot that holds no native lifetime using methods such as <see cref="GameInputReading.TryGetGamepadSnapshot"/>
    /// before returning. Internally a one-shot native callback is registered; upon completion or cancellation it is unregistered
    /// on a background thread, never by calling <see cref="GameInputCallbackRegistration.Dispose"/> synchronously on the native
    /// callback thread. An exception thrown by <paramref name="selector"/> faults the returned task.
    /// <paramref name="selector"/> 收到的 <see cref="GameInputReading"/> 只在回呼執行期間有效，
    /// 不可以把它本身、或任何指向其原生生命週期的參考當作 <paramref name="selector"/> 的回傳值往外傳遞；
    /// 應改用 <see cref="GameInputReading.TryGetGamepadSnapshot"/> 等方法轉換成不持有原生生命週期的快照後再回傳。
    /// 內部會註冊一次性原生回呼；完成或取消後會透過背景執行緒解除註冊，
    /// 不會在原生回呼執行緒中同步呼叫 <see cref="GameInputCallbackRegistration.Dispose"/>。
    /// <paramref name="selector"/> 拋出的例外會讓傳回的工作以該例外失敗。
    /// </remarks>
    /// <typeparam name="TResult">The converted safe result type. 轉換後的安全結果型別。</typeparam>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="selector">The delegate that converts the reading into a safe result while the native callback is still executing. 在原生回呼執行期間，把 reading 轉換為安全結果的委派。</param>
    /// <param name="cancellationToken">The cancellation token. 取消語彙。</param>
    /// <returns>A task that produces the converted result when the next matching reading arrives. 下一筆符合條件的 reading 到達時，產生轉換結果的工作。</returns>
    public Task<TResult> WaitForReadingAsync<TResult>(
        GameInputKind inputKind,
        GameInputDevice? device,
        Func<GameInputReading, TResult> selector,
        CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#else
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }
#endif

        return RunAwaitableCallback<TResult>(
            (onResult, onError) => RegisterReadingCallback(device, inputKind, reading =>
            {
                // selector 的例外必須讓等待中的工作失敗；若交給回呼包裝吞下，工作會永遠不會完成。
                TResult result;
                try
                {
                    result = selector(reading);
                }
                catch (Exception ex)
                {
                    onError(ex);
                    return;
                }

                onResult(result);
            }),
            cancellationToken,
            nameof(GameInputClient));
    }

    /// <summary>
    /// Consolidates the boilerplate of one-shot native callback plus cancellation token plus exception-based completion on
    /// dispose, shared by <see cref="WaitForReadingAsync{TResult}"/> and
    /// <see cref="GameInputDeviceManager.WaitForDeviceEventAsync(GameInputKind, GameInputDeviceStatus, CancellationToken)"/>.
    /// 收斂「一次性原生回呼 + 取消語彙 + Dispose 時以例外收尾」的樣板邏輯，供 <see cref="WaitForReadingAsync{TResult}"/>
    /// 與 <see cref="GameInputDeviceManager.WaitForDeviceEventAsync(GameInputKind, GameInputDeviceStatus, CancellationToken)"/> 共用。
    /// </summary>
    /// <typeparam name="TResult">The converted safe result type. 轉換後的安全結果型別。</typeparam>
    /// <param name="register">The delegate that registers the one-shot native callback; it receives <c>onResult</c> and <c>onError</c>, and the callback should finish converting the result before invoking <c>onResult</c>, or pass any conversion exception to <c>onError</c> so the task faults instead of never completing. 註冊一次性原生回呼的委派；會收到 <c>onResult</c> 與 <c>onError</c>，回呼應先把結果轉換完成再呼叫 <c>onResult</c>，轉換時的例外則交給 <c>onError</c>，讓工作失敗而不是永遠不會完成。</param>
    /// <param name="cancellationToken">The cancellation token. 取消語彙。</param>
    /// <param name="disposedObjectName">The object name to attach to the <see cref="ObjectDisposedException"/> when the caller is disposed while the wait is pending. 呼叫端在等待期間被釋放時，<see cref="ObjectDisposedException"/> 要標示的物件名稱。</param>
    /// <returns>A task that produces the converted result when the callback completes. 回呼完成時產生轉換結果的工作。</returns>
    internal Task<TResult> RunAwaitableCallback<TResult>(
        Func<Action<TResult>, Action<Exception>, GameInputCallbackRegistration> register,
        CancellationToken cancellationToken,
        string disposedObjectName)
    {
        TaskCompletionSource<TResult> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DeferredCallbackCompletion completion = new();
        Action cancelForDispose = () =>
        {
            completionSource.TrySetException(new ObjectDisposedException(disposedObjectName));
            completion.DisposeCancellationRegistrationForDispose();
        };
        RegisterPendingWait(cancelForDispose);

        GameInputCallbackRegistration registration;
        try
        {
            registration = register(
                result =>
                {
                    if (completionSource.TrySetResult(result))
                    {
                        UnregisterPendingWait(cancelForDispose);
                        completion.Complete();
                    }
                },
                exception =>
                {
                    if (completionSource.TrySetException(exception))
                    {
                        UnregisterPendingWait(cancelForDispose);
                        completion.Complete();
                    }
                });
        }
        catch
        {
            UnregisterPendingWait(cancelForDispose);
            throw;
        }
        completion.SetRegistration(registration);

        CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(() =>
        {
            if (completionSource.TrySetCanceled(cancellationToken))
            {
                UnregisterPendingWait(cancelForDispose);
                completion.Complete();
            }
        });
        completion.SetCancellationRegistration(cancellationRegistration);

        return completionSource.Task;
    }

    /// <summary>
    /// Asynchronously waits for the next gamepad reading and converts it into a snapshot that holds no native lifetime.
    /// 非同步等待下一筆 gamepad reading，並轉換為不持有原生生命週期的快照。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="cancellationToken">The cancellation token. 取消語彙。</param>
    /// <returns>A task that produces the next gamepad snapshot. 產生下一筆 gamepad 快照的工作。</returns>
    public Task<GamepadReadingSnapshot?> WaitForGamepadAsync(GameInputDevice? device = null, CancellationToken cancellationToken = default)
    {
        return WaitForReadingAsync(
            GameInputKind.GameInputKindGamepad,
            device,
            static reading => reading.TryGetGamepadSnapshot(out GamepadReadingSnapshot snapshot) ? snapshot : (GamepadReadingSnapshot?)null,
            cancellationToken);
    }

    /// <summary>
    /// Registers a device callback.
    /// 註冊裝置 callback。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="statusFilter">The device status to filter. 要篩選的裝置狀態。</param>
    /// <param name="enumerationKind">The device enumeration mode. 裝置列舉模式。</param>
    /// <param name="handler">The managed callback handler to register. 要註冊的 managed callback handler。</param>
    /// <returns>The callback registration used to unregister the callback. 用來解除註冊的 callback 註冊。</returns>
    public GameInputCallbackRegistration RegisterDeviceCallback(
        GameInputDevice? device,
        GameInputKind inputKind,
        GameInputDeviceStatus statusFilter,
        GameInputEnumerationKind enumerationKind,
        GameInputDeviceHandler handler)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(handler);
#else
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
#endif

        DeviceCallbackContext context = new(handler);
        GCHandle handle = GCHandle.Alloc(context);
        ulong token = 0;
        try
        {
            using ComLease<IGameInput> call = EnterNative();
            using ComLease<IGameInputDevice> deviceLease = device is null ? default : device.EnterNative();
            int hResult = call.Native.RegisterDeviceCallback(
                deviceLease.NativeOrNull,
                inputKind,
                statusFilter,
                enumerationKind,
                GCHandle.ToIntPtr(handle),
                DeviceCallbackPointer,
                out token);
            GameInputException.ThrowIfFailed(hResult);
            return AddRegistration(token, handle, context.Deactivate);
        }
        catch
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }

            throw;
        }
    }

    /// <summary>
    /// Registers a system button callback.
    /// 註冊 system button callback。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="buttonFilter">The system buttons to filter. 要篩選的 system button。</param>
    /// <param name="handler">The managed callback handler to register. 要註冊的 managed callback handler。</param>
    /// <returns>The callback registration used to unregister the callback. 用來解除註冊的 callback 註冊。</returns>
    public GameInputCallbackRegistration RegisterSystemButtonCallback(GameInputDevice? device, GameInputSystemButtons buttonFilter, GameInputSystemButtonHandler handler)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(handler);
#else
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
#endif

        SystemButtonCallbackContext context = new(handler);
        GCHandle handle = GCHandle.Alloc(context);
        ulong token = 0;
        try
        {
            using ComLease<IGameInput> call = EnterNative();
            using ComLease<IGameInputDevice> deviceLease = device is null ? default : device.EnterNative();
            int hResult = call.Native.RegisterSystemButtonCallback(
                deviceLease.NativeOrNull,
                buttonFilter,
                GCHandle.ToIntPtr(handle),
                SystemButtonCallbackPointer,
                out token);
            GameInputException.ThrowIfFailed(hResult);
            return AddRegistration(token, handle, context.Deactivate);
        }
        catch
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }

            throw;
        }
    }

    /// <summary>
    /// Registers a keyboard layout callback.
    /// 註冊鍵盤配置 callback。
    /// </summary>
    /// <param name="device">An optional GameInput device filter. 選用的 GameInput 裝置篩選。</param>
    /// <param name="handler">The managed callback handler to register. 要註冊的 managed callback handler。</param>
    /// <returns>The callback registration used to unregister the callback. 用來解除註冊的 callback 註冊。</returns>
    public GameInputCallbackRegistration RegisterKeyboardLayoutCallback(GameInputDevice? device, GameInputKeyboardLayoutHandler handler)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(handler);
#else
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
#endif

        KeyboardLayoutCallbackContext context = new(handler);
        GCHandle handle = GCHandle.Alloc(context);
        ulong token = 0;
        try
        {
            using ComLease<IGameInput> call = EnterNative();
            using ComLease<IGameInputDevice> deviceLease = device is null ? default : device.EnterNative();
            int hResult = call.Native.RegisterKeyboardLayoutCallback(
                deviceLease.NativeOrNull,
                GCHandle.ToIntPtr(handle),
                KeyboardLayoutCallbackPointer,
                out token);
            GameInputException.ThrowIfFailed(hResult);
            return AddRegistration(token, handle, context.Deactivate);
        }
        catch
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }

            throw;
        }
    }

    /// <summary>
    /// Releases the GameInput client and any callbacks not yet unregistered.
    /// 釋放 GameInput 用戶端與尚未解除註冊的 callback。
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// 有一個或多個回呼註冊因為在自身的原生回呼執行緒中被同步釋放而無法取消註冊；此時其餘資源仍會完成釋放。
    /// </exception>
    /// <remarks>
    /// Safe to call concurrently or repeatedly; only the first call performs disposal. Native calls already in progress on other
    /// threads (for example <see cref="EnumerateDevicesAsync"/>) keep the native object alive until they return, and the native
    /// object is released when the last of them finishes. Once the native object is released, child objects obtained from this
    /// client (devices, readings, dispatchers and so on) become invalid and their native calls fail with
    /// <see cref="GameInputHResult.ObjectNoLongerExists"/>; dispose them first and keep snapshots for data you still need.
    /// 可安全地並行或重複呼叫，只有第一次呼叫會執行釋放。其他執行緒上已在進行中的原生呼叫
    /// （例如 <see cref="EnumerateDevicesAsync"/>）會讓原生物件存活到呼叫返回，最後一個呼叫結束時才釋放原生物件。
    /// 原生物件釋放後，從這個用戶端取得的子物件（裝置、reading、dispatcher 等）都會失效，原生呼叫會以
    /// <see cref="GameInputHResult.ObjectNoLongerExists"/> 失敗；請先釋放子物件，需要保留的資料請先轉成快照。
    /// </remarks>
    public void Dispose()
    {
        // 先標記為釋放中，讓之後的公開呼叫與新註冊一律視為已釋放；
        // 回呼註冊的清理仍可透過 EnterNativeForCleanup 使用尚未歸零的原生參考。
        if (Interlocked.CompareExchange(ref _disposeState, 1, 0) != 0)
        {
            return;
        }

        Action[] pendingWaitCancellations;
        lock (_syncRoot)
        {
            pendingWaitCancellations = [.. _pendingWaitCancellations];
            _pendingWaitCancellations.Clear();
        }

        foreach (Action cancelForDispose in pendingWaitCancellations)
        {
            cancelForDispose();
        }

        GameInputCallbackRegistration[] registrations;
        lock (_syncRoot)
        {
            registrations = [.. _registrations];
            _registrations.Clear();
        }

        List<Exception>? disposeFailures = null;
        foreach (GameInputCallbackRegistration registration in registrations)
        {
            if (registration.DisposeSafely() is { } failure)
            {
                (disposeFailures ??= []).Add(failure);
            }
        }

        // SafeHandle 保證 ReleaseHandle 只執行一次，並延後到最後一個進行中的原生呼叫結束。
        _handle.Dispose();
        GC.SuppressFinalize(this);

        if (disposeFailures is not null)
        {
            throw new AggregateException("部分 callback 註冊無法釋放；其餘資源已完成釋放。", disposeFailures);
        }
    }

    /// <summary>
    /// Keeps one extra reference to the GameInput runtime's singleton root object for the rest of the process, so its reference
    /// count never drops to zero.
    /// 替 GameInput 執行階段的單例根物件保留一份額外參考直到處理序結束，讓參考計數永遠不會降到零。
    /// </summary>
    /// <remarks>
    /// <c>GameInputInitialize</c> returns the same singleton root to every caller. Measured with GameInput 3.5.274 and 3.5.278,
    /// the process crashes with an access violation when that root's last reference is released on one thread while another
    /// thread calls <c>GameInputInitialize</c> (for example one client being disposed while another is created), whereas holding
    /// an anchor reference survived more than 15,000 such overlaps. This matches the loader, which also keeps the runtime module
    /// loaded until the process exits.
    /// <c>GameInputInitialize</c> 對每個呼叫端都傳回同一個單例根物件。以 GameInput 3.5.274 與 3.5.278 實測，
    /// 一條執行緒釋放該根物件最後一份參考、同時另一條執行緒呼叫 <c>GameInputInitialize</c>（例如一個 client 釋放時另一個正在建立）
    /// 會讓處理序以存取違規崩潰；保留錨點參考後，超過 15,000 次同樣的重疊都正常。這與載入器讓執行階段模組常駐到處理序結束的做法一致。
    /// </remarks>
    /// <param name="root">The root object pointer returned by <c>GameInputInitialize</c>. <c>GameInputInitialize</c> 傳回的根物件指標。</param>
    private static void AnchorRuntimeRoot(IntPtr root)
    {
        if (Volatile.Read(ref s_anchoredRoot) != IntPtr.Zero)
        {
            return;
        }

        Marshal.AddRef(root);
        if (Interlocked.CompareExchange(ref s_anchoredRoot, root, IntPtr.Zero) != IntPtr.Zero)
        {
            // 其他執行緒已先保留錨點；歸還多取的參考。呼叫端的 SafeHandle 仍持有一份，這裡不會讓計數歸零。
            Marshal.Release(root);
        }
    }

    private GameInputCallbackRegistration AddRegistration(ulong token, GCHandle contextHandle, Action deactivateContext)
    {
        GameInputCallbackRegistration registration = new(
            token,
            contextHandle,
            deactivateContext,
            UnregisterCallback,
            RemoveRegistration,
            AcquireCleanupLease);

        bool disposed;
        lock (_syncRoot)
        {
            disposed = IsDisposeStarted;
            if (!disposed)
            {
                _registrations.Add(registration);
            }
        }

        if (disposed)
        {
            _ = registration.DisposeSafely();
        }

        return registration;
    }

    /// <summary>
    /// Takes a lease on the native root object that outlives the calling scope; returns the action that releases it, or
    /// <see langword="null"/> when the root object has already been released.
    /// 取得可跨越呼叫範圍的原生根物件租約；傳回歸還租約的動作，根物件已釋放時傳回 <see langword="null"/>。
    /// </summary>
    private Action? AcquireCleanupLease()
    {
        bool success = false;
        try
        {
            _handle.DangerousAddRef(ref success);
        }
        catch (ObjectDisposedException)
        {
            return null;
        }

        return success ? _handle.DangerousRelease : null;
    }

    private bool UnregisterCallback(ulong token)
    {
        using ComLease<IGameInput> call = EnterNativeForCleanup();
        return call.Native.UnregisterCallback(token);
    }

    private void RemoveRegistration(GameInputCallbackRegistration registration)
    {
        lock (_syncRoot)
        {
            _registrations.Remove(registration);
        }
    }

    /// <summary>
    /// Tracks a pending asynchronous wait so <see cref="Dispose"/> can complete it with an exception when releasing resources,
    /// preventing the caller's <see cref="Task"/> from staying pending forever.
    /// 追蹤一個尚未完成的非同步等待，讓 <see cref="Dispose"/> 能在釋放資源時把它以例外收尾，
    /// 避免呼叫端的 <see cref="Task"/> 永遠停在 pending 狀態。
    /// </summary>
    /// <param name="cancelForDispose">The delegate invoked during <see cref="Dispose"/> that completes the wait with an exception. 在 <see cref="Dispose"/> 時呼叫、讓等待以例外完成的委派。</param>
    internal void RegisterPendingWait(Action cancelForDispose)
    {
        bool disposed;
        lock (_syncRoot)
        {
            disposed = IsDisposeStarted;
            if (!disposed)
            {
                _pendingWaitCancellations.Add(cancelForDispose);
            }
        }

        if (disposed)
        {
            cancelForDispose();
        }
    }

    /// <summary>
    /// Stops tracking a wait registered by <see cref="RegisterPendingWait"/>; called when the wait completed through the normal
    /// path.
    /// 解除 <see cref="RegisterPendingWait"/> 追蹤的等待，等待已透過一般路徑完成時呼叫。
    /// </summary>
    /// <param name="cancelForDispose">The same delegate previously passed to <see cref="RegisterPendingWait"/>. 先前傳入 <see cref="RegisterPendingWait"/> 的同一個委派。</param>
    internal void UnregisterPendingWait(Action cancelForDispose)
    {
        lock (_syncRoot)
        {
            _pendingWaitCancellations.Remove(cancelForDispose);
        }
    }

    private static bool TryGetValue<TSnapshot>(TSnapshot? value, out TSnapshot snapshot)
        where TSnapshot : struct
    {
        snapshot = value.GetValueOrDefault();
        return value.HasValue;
    }

    private TSnapshot? GetCurrentSnapshot<TSnapshot>(
        GameInputKind inputKind,
        GameInputDevice? device,
        TryCreateReadingSnapshot<TSnapshot> tryCreateSnapshot)
        where TSnapshot : struct
    {
        // 快照讀取完即釋放，不會交給使用者，因此使用不配置 SafeHandle 的短命 reading。
        using GameInputReading? reading = GetCurrentReadingCore(inputKind, device, scoped: true);
        if (reading is null)
        {
            return null;
        }

        return tryCreateSnapshot(reading, out TSnapshot snapshot) ? snapshot : null;
    }

    private bool IsDisposeStarted
    {
        get
        {
            return Volatile.Read(ref _disposeState) != 0;
        }
    }

    /// <summary>
    /// Acquires a lease on the native object for a public call; fails once disposal has started.
    /// 為公開呼叫取得原生物件租約；開始釋放後即失敗。
    /// </summary>
    private ComLease<IGameInput> EnterNative()
    {
        if (IsDisposeStarted)
        {
            throw new ObjectDisposedException(nameof(GameInputClient));
        }

        return EnterNativeForCleanup();
    }

    /// <summary>
    /// Acquires a lease on the native object for callback cleanup; succeeds during disposal as long as the native object has not
    /// been released yet.
    /// 為回呼清理取得原生物件租約；只要原生物件尚未釋放，釋放期間仍可成功。
    /// </summary>
    private ComLease<IGameInput> EnterNativeForCleanup()
    {
        return _handle.Acquire<IGameInput>(nameof(GameInputClient));
    }

    /// <summary>
    /// Runs <paramref name="action"/> while holding a lease that keeps the native root object alive, so child objects obtained
    /// inside it stay valid even if <see cref="Dispose"/> runs concurrently.
    /// 在持有原生根物件租約的期間執行 <paramref name="action"/>，讓其中取得的子物件即使遇到並行的 <see cref="Dispose"/> 也保持有效。
    /// </summary>
    /// <typeparam name="TResult">The result type. 結果型別。</typeparam>
    /// <param name="action">The action to run. 要執行的動作。</param>
    /// <returns>The result of <paramref name="action"/>. <paramref name="action"/> 的結果。</returns>
    internal TResult WithNativeLease<TResult>(Func<TResult> action)
    {
        using ComLease<IGameInput> call = EnterNative();
        return action();
    }

    private delegate bool TryCreateReadingSnapshot<TSnapshot>(GameInputReading reading, out TSnapshot snapshot)
        where TSnapshot : struct;

#if NET8_0_OR_GREATER
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
#endif
    private static void OnReadingCallback(ulong callbackToken, IntPtr context, IGameInputReading reading)
    {
        GameInputCallbackThread.Enter();
        try
        {
            if (TryGetContext(context, out ReadingCallbackContext? callbackContext) && callbackContext!.TryGetHandler(out GameInputReadingHandler? handler))
            {
                using GameInputReading managedReading = WrapBorrowedReading(reading);
                handler(managedReading);
            }
        }
        catch (Exception ex)
        {
            RaiseUnhandledCallbackException(ex);
        }
        finally
        {
            GameInputCallbackThread.Exit();
        }
    }

#if NET8_0_OR_GREATER
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
#endif
    private static void OnDeviceCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus)
    {
        GameInputCallbackThread.Enter();
        try
        {
            if (TryGetContext(context, out DeviceEnumerationContext? enumerationContext))
            {
                GameInputDevice managedDevice = WrapBorrowedDevice(device);
                if (!enumerationContext!.TryAdd(managedDevice))
                {
                    // 列舉已結束並分離內容；遲到的回呼不能把 COM 參考留給終結器。
                    managedDevice.Dispose();
                }

                return;
            }

            if (TryGetContext(context, out DeviceCallbackContext? callbackContext) && callbackContext!.TryGetHandler(out GameInputDeviceHandler? handler))
            {
                using GameInputDevice managedDevice = WrapBorrowedDevice(device);
                handler(managedDevice, timestamp, currentStatus, previousStatus);
            }
        }
        catch (Exception ex)
        {
            RaiseUnhandledCallbackException(ex);
        }
        finally
        {
            GameInputCallbackThread.Exit();
        }
    }

#if NET8_0_OR_GREATER
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
#endif
    private static void OnSystemButtonCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, GameInputSystemButtons currentButtons, GameInputSystemButtons previousButtons)
    {
        GameInputCallbackThread.Enter();
        try
        {
            if (TryGetContext(context, out SystemButtonCallbackContext? callbackContext) && callbackContext!.TryGetHandler(out GameInputSystemButtonHandler? handler))
            {
                using GameInputDevice managedDevice = WrapBorrowedDevice(device);
                handler(managedDevice, timestamp, currentButtons, previousButtons);
            }
        }
        catch (Exception ex)
        {
            RaiseUnhandledCallbackException(ex);
        }
        finally
        {
            GameInputCallbackThread.Exit();
        }
    }

#if NET8_0_OR_GREATER
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
#endif
    private static void OnKeyboardLayoutCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, uint currentLayout, uint previousLayout)
    {
        GameInputCallbackThread.Enter();
        try
        {
            if (TryGetContext(context, out KeyboardLayoutCallbackContext? callbackContext) && callbackContext!.TryGetHandler(out GameInputKeyboardLayoutHandler? handler))
            {
                using GameInputDevice managedDevice = WrapBorrowedDevice(device);
                handler(managedDevice, timestamp, currentLayout, previousLayout);
            }
        }
        catch (Exception ex)
        {
            RaiseUnhandledCallbackException(ex);
        }
        finally
        {
            GameInputCallbackThread.Exit();
        }
    }

    internal static void RaiseUnhandledCallbackException(Exception exception)
    {
        try
        {
            UnhandledCallbackException?.Invoke(null, new GameInputCallbackExceptionEventArgs(exception));
        }
        catch
        {
            // 事件訂閱者拋出的例外同樣不可跨越原生 P/Invoke 邊界，於此吞下。
        }
    }

    /// <summary>
    /// Wraps a device pointer borrowed from a native callback into a wrapper that owns its own COM reference.
    /// 把原生回呼借用的裝置指標包裝成持有自身 COM 參考的包裝。
    /// </summary>
    /// <remarks>
    /// COM callback parameters are owned by the caller and are not AddRef'd for the callee (see "Rules for Managing Reference
    /// Counts"), so the wrapper must AddRef here to balance the <see cref="GameInputDevice.Dispose"/> Release; otherwise the device
    /// is over-released and the native heap is corrupted.
    /// COM 回呼參數由呼叫端擁有，不會替被呼叫端 AddRef（見「Rules for Managing Reference Counts」），
    /// 因此必須在此 AddRef 以平衡 <see cref="GameInputDevice.Dispose"/> 的 Release；否則裝置會被過度釋放並破壞原生堆積。
    /// </remarks>
    internal static GameInputDevice WrapBorrowedDevice(IGameInputDevice device)
    {
        device.AddRef();
        try
        {
            return new GameInputDevice(device);
        }
        catch
        {
            // 包裝建立失敗（例如記憶體不足）時歸還剛取得的參考，避免原生物件永遠無法釋放。
            device.Release();
            throw;
        }
    }

    /// <summary>
    /// Wraps a reading pointer borrowed from a native callback into a wrapper that owns its own COM reference.
    /// 把原生回呼借用的 reading 指標包裝成持有自身 COM 參考的包裝。
    /// </summary>
    /// <remarks>
    /// See <see cref="WrapBorrowedDevice"/> for the reference-counting rationale.
    /// 參考計數的理由請見 <see cref="WrapBorrowedDevice"/>。
    /// </remarks>
    internal static GameInputReading WrapBorrowedReading(IGameInputReading reading)
    {
        reading.AddRef();
        try
        {
            return new GameInputReading(reading);
        }
        catch
        {
            reading.Release();
            throw;
        }
    }

    private static bool TryGetContext<TContext>(IntPtr context, out TContext? callbackContext)
        where TContext : CallbackContext
    {
        callbackContext = null;
        if (context == IntPtr.Zero)
        {
            return false;
        }

        GCHandle handle = GCHandle.FromIntPtr(context);
        if (handle.Target is TContext target && target.IsActive)
        {
            callbackContext = target;
            return true;
        }

        return false;
    }

    internal abstract class CallbackContext
    {
        private volatile bool _isActive = true;

        public bool IsActive
        {
            get
            {
                return _isActive;
            }
        }

        /// <summary>
        /// Stops dispatching to this context. Derived contexts also drop their handler references here.
        /// 停止分派到此內容；衍生內容也會在此放掉處理常式參考。
        /// </summary>
        public virtual void Deactivate()
        {
            _isActive = false;
        }
    }

    /// <summary>
    /// A callback context that owns a user handler until it is deactivated.
    /// 在停用前持有使用者處理常式的回呼內容。
    /// </summary>
    /// <remarks>
    /// When <c>UnregisterCallback</c> returns false the GCHandle must stay allocated, which keeps this context alive for the rest
    /// of the process; dropping the handler on deactivation keeps whatever the handler captured from leaking with it.
    /// <c>UnregisterCallback</c> 傳回 false 時 GCHandle 必須保留，此內容會存活到處理序結束；停用時放掉處理常式，
    /// 可避免處理常式捕捉的物件一起洩漏。
    /// </remarks>
    internal abstract class HandlerCallbackContext<THandler>(THandler handler) : CallbackContext
        where THandler : Delegate
    {
        private THandler? _handler = handler;

        /// <summary>
        /// Gets the handler while the context is active; an in-flight callback keeps its own copy after deactivation.
        /// 內容仍啟用時取得處理常式；停用後進行中的回呼仍保有自己取得的副本。
        /// </summary>
        public bool TryGetHandler([NotNullWhen(true)] out THandler? handler)
        {
            handler = Volatile.Read(ref _handler);
            return handler is not null;
        }

        /// <inheritdoc />
        public override void Deactivate()
        {
            base.Deactivate();
            Volatile.Write(ref _handler, null);
        }
    }

    internal sealed class ReadingCallbackContext(GameInputReadingHandler handler) : HandlerCallbackContext<GameInputReadingHandler>(handler);

    internal sealed class DeviceCallbackContext(GameInputDeviceHandler handler) : HandlerCallbackContext<GameInputDeviceHandler>(handler);

    internal sealed class SystemButtonCallbackContext(GameInputSystemButtonHandler handler) : HandlerCallbackContext<GameInputSystemButtonHandler>(handler);

    internal sealed class KeyboardLayoutCallbackContext(GameInputKeyboardLayoutHandler handler) : HandlerCallbackContext<GameInputKeyboardLayoutHandler>(handler);

    internal sealed class DeviceEnumerationContext : CallbackContext
    {
#if NET9_0_OR_GREATER
        private readonly System.Threading.Lock _syncRoot = new();
#else
        private readonly object _syncRoot = new();
#endif
        private List<GameInputDevice>? _devices = [];

        /// <summary>
        /// Adds a device collected by the callback; returns false after the context has been detached.
        /// 加入回呼收集到的裝置；內容已分離後傳回 false。
        /// </summary>
        public bool TryAdd(GameInputDevice device)
        {
            lock (_syncRoot)
            {
                if (_devices is null)
                {
                    return false;
                }

                _devices.Add(device);
                return true;
            }
        }

        /// <summary>
        /// Detaches and returns the collected devices; later calls return an empty array.
        /// 分離並傳回已收集的裝置；之後的呼叫傳回空陣列。
        /// </summary>
        public GameInputDevice[] Detach()
        {
            lock (_syncRoot)
            {
                GameInputDevice[] devices = _devices?.ToArray() ?? [];
                _devices = null;
                return devices;
            }
        }
    }
}
