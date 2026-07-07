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

#if !NET10_0_OR_GREATER
    private static readonly GameInputReadingCallback s_readingCallback = OnReadingCallback;
    private static readonly GameInputDeviceCallback s_deviceCallback = OnDeviceCallback;
    private static readonly GameInputSystemButtonCallback s_systemButtonCallback = OnSystemButtonCallback;
    private static readonly GameInputKeyboardLayoutCallback s_keyboardLayoutCallback = OnKeyboardLayoutCallback;
#endif

#if NET10_0_OR_GREATER
    private readonly System.Threading.Lock _syncRoot = new();
#else
    private readonly object _syncRoot = new();
#endif
    private readonly List<GameInputCallbackRegistration> _registrations = [];
    private readonly List<Action> _pendingWaitCancellations = [];
    private IGameInput? _native;
    private bool _disposed;

    private GameInputClient(IGameInput native)
    {
        _native = native;
    }

    /// <summary>
    /// Creates a GameInput v3 client.
    /// 建立 GameInput v3 用戶端。
    /// </summary>
    /// <exception cref="GameInputException">GameInput initialization failed. GameInput 初始化失敗。</exception>
    /// <returns>The newly created <see cref="GameInputClient"/> instance. 新建立的 <see cref="GameInputClient"/> 執行個體。</returns>
    public static GameInputClient Create()
    {
        Guid iid = GameInputIids.IGameInput;
        int hResult = GameInputNativeMethods.GameInputInitialize(ref iid, out IntPtr nativePointer);
        GameInputException.ThrowIfFailed(hResult);

        try
        {
#if NET10_0_OR_GREATER
            IGameInput native = new(nativePointer);
            native.AddRef();
#else
            IGameInput native = (IGameInput)Marshal.GetObjectForIUnknown(nativePointer);
#endif
            return new GameInputClient(native);
        }
        finally
        {
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
            }
        }
    }

    /// <summary>
    /// Gets the current GameInput timestamp.
    /// 取得 GameInput 目前時間戳記。
    /// </summary>
    /// <returns>The current GameInput timestamp. 目前的 GameInput 時間戳記。</returns>
    public ulong GetCurrentTimestamp()
    {
        return Native.GetCurrentTimestamp();
    }

    /// <summary>
    /// Sets the GameInput focus policy.
    /// 設定 GameInput 焦點政策。
    /// </summary>
    /// <param name="policy">The focus policy to apply. 要套用的焦點政策。</param>
    public void SetFocusPolicy(GameInputFocusPolicy policy)
    {
        Native.SetFocusPolicy(policy);
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
            static (GameInputReading reading, out GamepadReadingSnapshot? snapshot) => reading.TryGetGamepadSnapshot(out snapshot));
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
            static (GameInputReading reading, out KeyboardReadingSnapshot? snapshot) => reading.TryGetKeyboardSnapshot(out snapshot));
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
            static (GameInputReading reading, out MouseReadingSnapshot? snapshot) => reading.TryGetMouseSnapshot(out snapshot));
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
            static (GameInputReading reading, out SensorsReadingSnapshot? snapshot) => reading.TryGetSensorsSnapshot(out snapshot));
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
            static (GameInputReading reading, out ControllerReadingSnapshot? snapshot) => reading.TryGetControllerSnapshot(out snapshot));
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
            static (GameInputReading reading, out ArcadeStickReadingSnapshot? snapshot) => reading.TryGetArcadeStickSnapshot(out snapshot));
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
            static (GameInputReading reading, out FlightStickReadingSnapshot? snapshot) => reading.TryGetFlightStickSnapshot(out snapshot));
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
            static (GameInputReading reading, out RacingWheelReadingSnapshot? snapshot) => reading.TryGetRacingWheelSnapshot(out snapshot));
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
            static (GameInputReading reading, out RawDeviceReportSnapshot? snapshot) => reading.TryGetRawReportSnapshot(out snapshot));
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
        int hResult = Native.GetCurrentReading(inputKind, device?.NativeInterface, out IGameInputReading? nativeReading);
        if (hResult == GameInputHResult.ReadingNotFound || hResult == GameInputHResult.InputKindNotPresent)
        {
            return null;
        }

        GameInputException.ThrowIfFailed(hResult);
        return nativeReading is { } reading ? new GameInputReading(reading) : null;
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
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(referenceReading);
#else
        if (referenceReading is null)
        {
            throw new ArgumentNullException(nameof(referenceReading));
        }
#endif

        int hResult = Native.GetNextReading(referenceReading.NativeInterface, inputKind, device?.NativeInterface, out IGameInputReading? nativeReading);
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
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(referenceReading);
#else
        if (referenceReading is null)
        {
            throw new ArgumentNullException(nameof(referenceReading));
        }
#endif

        int hResult = Native.GetPreviousReading(referenceReading.NativeInterface, inputKind, device?.NativeInterface, out IGameInputReading? nativeReading);
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
        int hResult = Native.CreateDispatcher(out IGameInputDispatcher? dispatcher);
        GameInputException.ThrowIfFailed(hResult);
        return dispatcher is { } dispatcherValue
            ? new GameInputDispatcher(dispatcherValue)
            : throw new GameInputException(GameInputHResult.ObjectNoLongerExists);
    }

    /// <summary>
    /// Finds a device by its device ID.
    /// 依裝置 ID 尋找裝置。
    /// </summary>
    /// <remarks>
    /// Empirically observed: within the very short window right after <see cref="CreateAggregateDevice"/> while native device
    /// registration has not finished, calling this method immediately with the freshly obtained <see cref="AppLocalDeviceId"/>
    /// can occasionally trigger an access violation (<c>0xC0000005</c>) inside the native GameInput runtime that managed
    /// exception handling cannot catch, instead of cleanly returning a device-not-found HRESULT; querying a device ID that never
    /// existed does not have this problem. The official documentation states that aggregate devices emit status notifications
    /// through callbacks registered via <see cref="RegisterDeviceCallback"/>, so to query a freshly created aggregate device, the
    /// correct approach is to first wait for its status notification using <see cref="RegisterDeviceCallback"/> (or
    /// <see cref="GameInputDeviceManager.DeviceChanged"/>) instead of guessing a delay.
    /// 實測觀察到：緊接在 <see cref="CreateAggregateDevice"/> 之後、原生裝置註冊尚未完成的極短暫時間窗內，立即用剛取得的
    /// <see cref="AppLocalDeviceId"/> 呼叫這個方法，偶爾會在原生 GameInput 執行階段觸發無法被 managed 例外攔截的存取違規
    /// （<c>0xC0000005</c>），而非乾淨地回傳「找不到裝置」的 HRESULT；查詢一個從未存在過的裝置 ID 則不會有這個問題。
    /// 官方文件說明聚合裝置會透過 <see cref="RegisterDeviceCallback"/> 註冊的回呼發出狀態通知，因此若要查詢剛建立的聚合裝置，
    /// 正確做法是先用 <see cref="RegisterDeviceCallback"/>（或 <see cref="GameInputDeviceManager.DeviceChanged"/>）
    /// 等到該裝置的狀態通知後再查詢，而不是猜測一個延遲時間。
    /// </remarks>
    /// <param name="deviceId">The GameInput device identifier. GameInput 裝置識別值。</param>
    /// <returns>The matching device wrapper. 符合的裝置包裝。</returns>
    public GameInputDevice FindDeviceFromId(in AppLocalDeviceId deviceId)
    {
        // 原生 API 的參數是唯讀（const APP_LOCAL_DEVICE_ID*），高階簽章用 in 符合 C# 慣用法；
        // 產生式互通層簽章為 ref，複製到區域變數轉交。
        AppLocalDeviceId local = deviceId;
        int hResult = Native.FindDeviceFromId(ref local, out IGameInputDevice? device);
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

        int hResult = Native.FindDeviceFromPlatformString(value, out IGameInputDevice? device);
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
        DeviceEnumerationContext context = new();
        GCHandle contextHandle = GCHandle.Alloc(context);
        ulong token = 0;
        try
        {
#if NET10_0_OR_GREATER
            int hResult;
            unsafe
            {
                hResult = Native.RegisterDeviceCallback(
                    device: null,
                    inputKind,
                    statusFilter,
                    GameInputEnumerationKind.GameInputBlockingEnumeration,
                    GCHandle.ToIntPtr(contextHandle),
                    (IntPtr)(delegate* unmanaged[Stdcall]<ulong, IntPtr, IGameInputDevice, ulong, GameInputDeviceStatus, GameInputDeviceStatus, void>)&OnDeviceCallback,
                    out token);
            }
#else
            int hResult = Native.RegisterDeviceCallback(
                device: null,
                inputKind,
                statusFilter,
                GameInputEnumerationKind.GameInputBlockingEnumeration,
                GCHandle.ToIntPtr(contextHandle),
                s_deviceCallback,
                out token);
#endif

            GameInputException.ThrowIfFailed(hResult);
            return context.Devices.ToArray();
        }
        finally
        {
            if (token != 0)
            {
                Native.StopCallback(token);
                Native.UnregisterCallback(token);
            }

            if (contextHandle.IsAllocated)
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
    /// <remarks>
    /// The returned <see cref="AppLocalDeviceId"/> should not be passed to <see cref="FindDeviceFromId"/> immediately; see that
    /// method's documentation for the known native timing hazard.
    /// 傳回的 <see cref="AppLocalDeviceId"/> 不建議立即傳入 <see cref="FindDeviceFromId"/> 查詢，詳見該方法文件說明的
    /// 已知原生時序風險。
    /// </remarks>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <returns>The device identifier of the aggregate device. 聚合裝置的裝置識別值。</returns>
    public AppLocalDeviceId CreateAggregateDevice(GameInputKind inputKind)
    {
        int hResult = Native.CreateAggregateDevice(inputKind, out AppLocalDeviceId deviceId);
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
        int hResult = Native.DisableAggregateDevice(ref local);
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
#if NET10_0_OR_GREATER
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
#if NET10_0_OR_GREATER
            int hResult;
            unsafe
            {
                hResult = Native.RegisterReadingCallback(
                    device?.NativeInterface,
                    inputKind,
                    GCHandle.ToIntPtr(handle),
                    (IntPtr)(delegate* unmanaged[Stdcall]<ulong, IntPtr, IGameInputReading, void>)&OnReadingCallback,
                    out token);
            }
#else
            int hResult = Native.RegisterReadingCallback(device?.NativeInterface, inputKind, GCHandle.ToIntPtr(handle), s_readingCallback, out token);
#endif
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
    /// callback thread.
    /// <paramref name="selector"/> 收到的 <see cref="GameInputReading"/> 只在回呼執行期間有效，
    /// 不可以把它本身、或任何指向其原生生命週期的參考當作 <paramref name="selector"/> 的回傳值往外傳遞；
    /// 應改用 <see cref="GameInputReading.TryGetGamepadSnapshot"/> 等方法轉換成不持有原生生命週期的快照後再回傳。
    /// 內部會註冊一次性原生回呼；完成或取消後會透過背景執行緒解除註冊，
    /// 不會在原生回呼執行緒中同步呼叫 <see cref="GameInputCallbackRegistration.Dispose"/>。
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
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(selector);
#else
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }
#endif

        return RunAwaitableCallback<TResult>(
            onResult => RegisterReadingCallback(device, inputKind, reading => onResult(selector(reading))),
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
    /// <param name="register">The delegate that registers the one-shot native callback; after receiving <c>onResult</c>, the callback should finish converting the result before invoking it. 註冊一次性原生回呼的委派；收到 <c>onResult</c> 後應在回呼內把結果轉換完成再呼叫它。</param>
    /// <param name="cancellationToken">The cancellation token. 取消語彙。</param>
    /// <param name="disposedObjectName">The object name to attach to the <see cref="ObjectDisposedException"/> when the caller is disposed while the wait is pending. 呼叫端在等待期間被釋放時，<see cref="ObjectDisposedException"/> 要標示的物件名稱。</param>
    /// <returns>A task that produces the converted result when the callback completes. 回呼完成時產生轉換結果的工作。</returns>
    internal Task<TResult> RunAwaitableCallback<TResult>(
        Func<Action<TResult>, GameInputCallbackRegistration> register,
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
            registration = register(result =>
            {
                if (completionSource.TrySetResult(result))
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
            static reading => reading.TryGetGamepadSnapshot(out GamepadReadingSnapshot? snapshot) ? snapshot : null,
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
#if NET10_0_OR_GREATER
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
#if NET10_0_OR_GREATER
            int hResult;
            unsafe
            {
                hResult = Native.RegisterDeviceCallback(
                    device?.NativeInterface,
                    inputKind,
                    statusFilter,
                    enumerationKind,
                    GCHandle.ToIntPtr(handle),
                    (IntPtr)(delegate* unmanaged[Stdcall]<ulong, IntPtr, IGameInputDevice, ulong, GameInputDeviceStatus, GameInputDeviceStatus, void>)&OnDeviceCallback,
                    out token);
            }
#else
            int hResult = Native.RegisterDeviceCallback(device?.NativeInterface, inputKind, statusFilter, enumerationKind, GCHandle.ToIntPtr(handle), s_deviceCallback, out token);
#endif
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
#if NET10_0_OR_GREATER
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
#if NET10_0_OR_GREATER
            int hResult;
            unsafe
            {
                hResult = Native.RegisterSystemButtonCallback(
                    device?.NativeInterface,
                    buttonFilter,
                    GCHandle.ToIntPtr(handle),
                    (IntPtr)(delegate* unmanaged[Stdcall]<ulong, IntPtr, IGameInputDevice, ulong, GameInputSystemButtons, GameInputSystemButtons, void>)&OnSystemButtonCallback,
                    out token);
            }
#else
            int hResult = Native.RegisterSystemButtonCallback(device?.NativeInterface, buttonFilter, GCHandle.ToIntPtr(handle), s_systemButtonCallback, out token);
#endif
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
#if NET10_0_OR_GREATER
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
#if NET10_0_OR_GREATER
            int hResult;
            unsafe
            {
                hResult = Native.RegisterKeyboardLayoutCallback(
                    device?.NativeInterface,
                    GCHandle.ToIntPtr(handle),
                    (IntPtr)(delegate* unmanaged[Stdcall]<ulong, IntPtr, IGameInputDevice, ulong, uint, uint, void>)&OnKeyboardLayoutCallback,
                    out token);
            }
#else
            int hResult = Native.RegisterKeyboardLayoutCallback(device?.NativeInterface, GCHandle.ToIntPtr(handle), s_keyboardLayoutCallback, out token);
#endif
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
    public void Dispose()
    {
        if (_disposed)
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

        if (_native is not null)
        {
#if NET10_0_OR_GREATER
            _native.Value.Release();
#else
            Marshal.ReleaseComObject(_native);
#endif
            _native = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);

        if (disposeFailures is not null)
        {
            throw new AggregateException("部分 callback 註冊無法釋放；其餘資源已完成釋放。", disposeFailures);
        }
    }

    private GameInputCallbackRegistration AddRegistration(ulong token, GCHandle contextHandle, Action deactivateContext)
    {
        GameInputCallbackRegistration registration = new(
            token,
            contextHandle,
            deactivateContext,
            StopCallback,
            UnregisterCallback,
            RemoveRegistration);

        bool disposed;
        lock (_syncRoot)
        {
            disposed = _disposed;
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

    private void StopCallback(ulong token)
    {
        Native.StopCallback(token);
    }

    private bool UnregisterCallback(ulong token)
    {
        return Native.UnregisterCallback(token);
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
            disposed = _disposed;
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

    private TSnapshot? GetCurrentSnapshot<TSnapshot>(
        GameInputKind inputKind,
        GameInputDevice? device,
        TryCreateReadingSnapshot<TSnapshot> tryCreateSnapshot)
        where TSnapshot : struct
    {
        using GameInputReading? reading = GetCurrentReading(inputKind, device);
        if (reading is null)
        {
            return null;
        }

        return tryCreateSnapshot(reading, out TSnapshot? snapshot) ? snapshot : null;
    }

    private IGameInput Native
    {
        get
        {
            return _disposed
                ? throw new ObjectDisposedException(nameof(GameInputClient))
                : _native ?? throw new ObjectDisposedException(nameof(GameInputClient));
        }
    }

    private delegate bool TryCreateReadingSnapshot<TSnapshot>(GameInputReading reading, out TSnapshot? snapshot)
        where TSnapshot : struct;

#if NET10_0_OR_GREATER
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
#endif
    private static void OnReadingCallback(ulong callbackToken, IntPtr context, IGameInputReading reading)
    {
        GameInputCallbackThread.Enter();
        try
        {
            if (TryGetContext(context, out ReadingCallbackContext? callbackContext))
            {
                using GameInputReading managedReading = new(reading);
                callbackContext!.Handler(managedReading);
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

#if NET10_0_OR_GREATER
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
#endif
    private static void OnDeviceCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus)
    {
        GameInputCallbackThread.Enter();
        try
        {
            if (TryGetContext(context, out DeviceEnumerationContext? enumerationContext))
            {
                enumerationContext!.Devices.Add(new GameInputDevice(device));
                return;
            }

            if (TryGetContext(context, out DeviceCallbackContext? callbackContext))
            {
                using GameInputDevice managedDevice = new(device);
                callbackContext!.Handler(managedDevice, timestamp, currentStatus, previousStatus);
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

#if NET10_0_OR_GREATER
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
#endif
    private static void OnSystemButtonCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, GameInputSystemButtons currentButtons, GameInputSystemButtons previousButtons)
    {
        GameInputCallbackThread.Enter();
        try
        {
            if (TryGetContext(context, out SystemButtonCallbackContext? callbackContext))
            {
                using GameInputDevice managedDevice = new(device);
                callbackContext!.Handler(managedDevice, timestamp, currentButtons, previousButtons);
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

#if NET10_0_OR_GREATER
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
#endif
    private static void OnKeyboardLayoutCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, uint currentLayout, uint previousLayout)
    {
        GameInputCallbackThread.Enter();
        try
        {
            if (TryGetContext(context, out KeyboardLayoutCallbackContext? callbackContext))
            {
                using GameInputDevice managedDevice = new(device);
                callbackContext!.Handler(managedDevice, timestamp, currentLayout, previousLayout);
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

    private abstract class CallbackContext
    {
        public bool IsActive { get; private set; } = true;

        /// <summary>
        /// Provides the public Deactivate API.
        /// 提供 Deactivate 公開 API。
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }
    }

    private sealed class ReadingCallbackContext(GameInputReadingHandler handler) : CallbackContext
    {
        public GameInputReadingHandler Handler { get; } = handler;
    }

    private sealed class DeviceCallbackContext(GameInputDeviceHandler handler) : CallbackContext
    {
        public GameInputDeviceHandler Handler { get; } = handler;
    }

    private sealed class SystemButtonCallbackContext(GameInputSystemButtonHandler handler) : CallbackContext
    {
        public GameInputSystemButtonHandler Handler { get; } = handler;
    }

    private sealed class KeyboardLayoutCallbackContext(GameInputKeyboardLayoutHandler handler) : CallbackContext
    {
        public GameInputKeyboardLayoutHandler Handler { get; } = handler;
    }

    private sealed class DeviceEnumerationContext : CallbackContext
    {
        public List<GameInputDevice> Devices { get; } = [];
    }
}
