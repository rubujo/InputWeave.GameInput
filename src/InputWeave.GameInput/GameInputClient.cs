using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// GameInput 的高階 C# 入口點。
/// </summary>
public sealed class GameInputClient : IDisposable
{
    private static readonly GameInputReadingCallback s_readingCallback = OnReadingCallback;
    private static readonly GameInputDeviceCallback s_deviceCallback = OnDeviceCallback;
    private static readonly GameInputSystemButtonCallback s_systemButtonCallback = OnSystemButtonCallback;
    private static readonly GameInputKeyboardLayoutCallback s_keyboardLayoutCallback = OnKeyboardLayoutCallback;

#if NET10_0_OR_GREATER
    private readonly System.Threading.Lock _syncRoot = new();
#else
    private readonly object _syncRoot = new();
#endif
    private readonly List<GameInputCallbackRegistration> _registrations = [];
    private IGameInput? _native;
    private bool _disposed;

    private GameInputClient(IGameInput native)
    {
        _native = native;
    }

    /// <summary>
    /// 建立 GameInput v3 用戶端。
    /// </summary>
    /// <exception cref="GameInputException">GameInput 初始化失敗。</exception>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public static GameInputClient Create()
    {
        Guid iid = GameInputIids.IGameInput;
        int hResult = GameInputNativeMethods.GameInputInitialize(ref iid, out IntPtr nativePointer);
        GameInputException.ThrowIfFailed(hResult);

        try
        {
            IGameInput native = (IGameInput)Marshal.GetObjectForIUnknown(nativePointer);
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
    /// 取得 GameInput 目前時間戳記。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public ulong GetCurrentTimestamp()
    {
        return Native.GetCurrentTimestamp();
    }

    /// <summary>
    /// 設定 GameInput 焦點政策。
    /// </summary>
    /// <param name="policy">要套用的焦點政策。</param>
    public void SetFocusPolicy(GameInputFocusPolicy policy)
    {
        Native.SetFocusPolicy(policy);
    }

    /// <summary>
    /// 取得目前 gamepad 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GamepadReadingSnapshot? GetCurrentGamepad(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindGamepad,
            device,
            static (GameInputReading reading, out GamepadReadingSnapshot? snapshot) => reading.TryGetGamepadSnapshot(out snapshot));
    }

    /// <summary>
    /// 取得目前 keyboard 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public KeyboardReadingSnapshot? GetCurrentKeyboard(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindKeyboard,
            device,
            static (GameInputReading reading, out KeyboardReadingSnapshot? snapshot) => reading.TryGetKeyboardSnapshot(out snapshot));
    }

    /// <summary>
    /// 取得目前 mouse 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public MouseReadingSnapshot? GetCurrentMouse(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindMouse,
            device,
            static (GameInputReading reading, out MouseReadingSnapshot? snapshot) => reading.TryGetMouseSnapshot(out snapshot));
    }

    /// <summary>
    /// 取得目前 sensors 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public SensorsReadingSnapshot? GetCurrentSensors(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindSensors,
            device,
            static (GameInputReading reading, out SensorsReadingSnapshot? snapshot) => reading.TryGetSensorsSnapshot(out snapshot));
    }

    /// <summary>
    /// 取得目前一般 controller 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public ControllerReadingSnapshot? GetCurrentController(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindController,
            device,
            static (GameInputReading reading, out ControllerReadingSnapshot? snapshot) => reading.TryGetControllerSnapshot(out snapshot));
    }

    /// <summary>
    /// 取得目前 arcade stick 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public ArcadeStickReadingSnapshot? GetCurrentArcadeStick(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindArcadeStick,
            device,
            static (GameInputReading reading, out ArcadeStickReadingSnapshot? snapshot) => reading.TryGetArcadeStickSnapshot(out snapshot));
    }

    /// <summary>
    /// 取得目前 flight stick 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public FlightStickReadingSnapshot? GetCurrentFlightStick(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindFlightStick,
            device,
            static (GameInputReading reading, out FlightStickReadingSnapshot? snapshot) => reading.TryGetFlightStickSnapshot(out snapshot));
    }

    /// <summary>
    /// 取得目前 racing wheel 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public RacingWheelReadingSnapshot? GetCurrentRacingWheel(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindRacingWheel,
            device,
            static (GameInputReading reading, out RacingWheelReadingSnapshot? snapshot) => reading.TryGetRacingWheelSnapshot(out snapshot));
    }

    /// <summary>
    /// 取得目前 raw device report 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public RawDeviceReportSnapshot? GetCurrentRawReport(GameInputDevice? device = null)
    {
        return GetCurrentSnapshot(
            GameInputKind.GameInputKindRawDeviceReport,
            device,
            static (GameInputReading reading, out RawDeviceReportSnapshot? snapshot) => reading.TryGetRawReportSnapshot(out snapshot));
    }

    /// <summary>
    /// 取得目前指定種類的低階讀取資料。
    /// </summary>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputReading? GetCurrentReading(GameInputKind inputKind, GameInputDevice? device = null)
    {
        int hResult = Native.GetCurrentReading(inputKind, device?.NativeInterface, out IGameInputReading? nativeReading);
        if (hResult == GameInputHResult.ReadingNotFound || hResult == GameInputHResult.InputKindNotPresent)
        {
            return null;
        }

        GameInputException.ThrowIfFailed(hResult);
        return nativeReading is null ? null : new GameInputReading(nativeReading);
    }

    /// <summary>
    /// 取得指定參考 reading 之後的 reading。
    /// </summary>
    /// <param name="referenceReading">作為查詢基準的 reading。</param>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
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
        return nativeReading is null ? null : new GameInputReading(nativeReading);
    }

    /// <summary>
    /// 取得指定參考 reading 之前的 reading。
    /// </summary>
    /// <param name="referenceReading">作為查詢基準的 reading。</param>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
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
        return nativeReading is null ? null : new GameInputReading(nativeReading);
    }

    /// <summary>
    /// 建立 GameInput dispatcher。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputDispatcher CreateDispatcher()
    {
        int hResult = Native.CreateDispatcher(out IGameInputDispatcher? dispatcher);
        GameInputException.ThrowIfFailed(hResult);
        return dispatcher is null
            ? throw new GameInputException(GameInputHResult.ObjectNoLongerExists)
            : new GameInputDispatcher(dispatcher);
    }

    /// <summary>
    /// 依裝置 ID 尋找裝置。
    /// </summary>
    /// <param name="deviceId">GameInput 裝置識別值。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputDevice FindDeviceFromId(ref AppLocalDeviceId deviceId)
    {
        int hResult = Native.FindDeviceFromId(ref deviceId, out IGameInputDevice? device);
        GameInputException.ThrowIfFailed(hResult);
        return device is null
            ? throw new GameInputException(GameInputHResult.DeviceNotFound)
            : new GameInputDevice(device);
    }

    /// <summary>
    /// 依平台字串尋找裝置。
    /// </summary>
    /// <param name="value">要傳入的值。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputDevice FindDeviceFromPlatformString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("平台字串不可為空白。", nameof(value));
        }

        int hResult = Native.FindDeviceFromPlatformString(value, out IGameInputDevice? device);
        GameInputException.ThrowIfFailed(hResult);
        return device is null
            ? throw new GameInputException(GameInputHResult.DeviceNotFound)
            : new GameInputDevice(device);
    }

    /// <summary>
    /// 列舉目前符合條件的裝置。
    /// </summary>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="statusFilter">要篩選的裝置狀態。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public IReadOnlyList<GameInputDevice> EnumerateDevices(GameInputKind inputKind, GameInputDeviceStatus statusFilter = GameInputDeviceStatus.GameInputDeviceConnected)
    {
        DeviceEnumerationContext context = new();
        GCHandle contextHandle = GCHandle.Alloc(context);
        ulong token = 0;
        try
        {
            int hResult = Native.RegisterDeviceCallback(
                device: null,
                inputKind,
                statusFilter,
                GameInputEnumerationKind.GameInputBlockingEnumeration,
                GCHandle.ToIntPtr(contextHandle),
                s_deviceCallback,
                out token);

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
    /// 建立聚合裝置。
    /// </summary>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public AppLocalDeviceId CreateAggregateDevice(GameInputKind inputKind)
    {
        int hResult = Native.CreateAggregateDevice(inputKind, out AppLocalDeviceId deviceId);
        GameInputException.ThrowIfFailed(hResult);
        return deviceId;
    }

    /// <summary>
    /// 停用聚合裝置。
    /// </summary>
    /// <param name="deviceId">GameInput 裝置識別值。</param>
    public void DisableAggregateDevice(ref AppLocalDeviceId deviceId)
    {
        int hResult = Native.DisableAggregateDevice(ref deviceId);
        GameInputException.ThrowIfFailed(hResult);
    }

    /// <summary>
    /// 註冊 reading callback。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="handler">要註冊的 managed callback handler。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
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
            int hResult = Native.RegisterReadingCallback(device?.NativeInterface, inputKind, GCHandle.ToIntPtr(handle), s_readingCallback, out token);
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
    /// 註冊裝置 callback。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="statusFilter">要篩選的裝置狀態。</param>
    /// <param name="enumerationKind">裝置列舉模式。</param>
    /// <param name="handler">要註冊的 managed callback handler。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
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
            int hResult = Native.RegisterDeviceCallback(device?.NativeInterface, inputKind, statusFilter, enumerationKind, GCHandle.ToIntPtr(handle), s_deviceCallback, out token);
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
    /// 註冊 system button callback。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <param name="buttonFilter">要篩選的 system button。</param>
    /// <param name="handler">要註冊的 managed callback handler。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
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
            int hResult = Native.RegisterSystemButtonCallback(device?.NativeInterface, buttonFilter, GCHandle.ToIntPtr(handle), s_systemButtonCallback, out token);
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
    /// 註冊鍵盤配置 callback。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <param name="handler">要註冊的 managed callback handler。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
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
            int hResult = Native.RegisterKeyboardLayoutCallback(device?.NativeInterface, GCHandle.ToIntPtr(handle), s_keyboardLayoutCallback, out token);
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
    /// 釋放 GameInput 用戶端與尚未解除註冊的 callback。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        GameInputCallbackRegistration[] registrations;
        lock (_syncRoot)
        {
            registrations = [.. _registrations];
            _registrations.Clear();
        }

        foreach (GameInputCallbackRegistration registration in registrations)
        {
            registration.Dispose();
        }

        if (_native is not null)
        {
            Marshal.ReleaseComObject(_native);
            _native = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
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

        lock (_syncRoot)
        {
            _registrations.Add(registration);
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

    private TSnapshot? GetCurrentSnapshot<TSnapshot>(
        GameInputKind inputKind,
        GameInputDevice? device,
        TryCreateReadingSnapshot<TSnapshot> tryCreateSnapshot)
        where TSnapshot : class
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
        where TSnapshot : class;

    private static void OnReadingCallback(ulong callbackToken, IntPtr context, IGameInputReading reading)
    {
        if (TryGetContext(context, out ReadingCallbackContext? callbackContext))
        {
            using GameInputReading managedReading = new(reading);
            callbackContext!.Handler(managedReading);
        }
    }

    private static void OnDeviceCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus)
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

    private static void OnSystemButtonCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, GameInputSystemButtons currentButtons, GameInputSystemButtons previousButtons)
    {
        if (TryGetContext(context, out SystemButtonCallbackContext? callbackContext))
        {
            using GameInputDevice managedDevice = new(device);
            callbackContext!.Handler(managedDevice, timestamp, currentButtons, previousButtons);
        }
    }

    private static void OnKeyboardLayoutCallback(ulong callbackToken, IntPtr context, IGameInputDevice device, ulong timestamp, uint currentLayout, uint previousLayout)
    {
        if (TryGetContext(context, out KeyboardLayoutCallbackContext? callbackContext))
        {
            using GameInputDevice managedDevice = new(device);
            callbackContext!.Handler(managedDevice, timestamp, currentLayout, previousLayout);
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
