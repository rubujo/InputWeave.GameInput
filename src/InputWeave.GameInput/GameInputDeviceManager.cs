using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// 管理 GameInput 裝置快取與事件佇列的高階入口。
/// </summary>
public sealed class GameInputDeviceManager : IDisposable
{
    private static readonly GameInputKind s_defaultInputKinds =
        GameInputKind.GameInputKindRawDeviceReport
        | GameInputKind.GameInputKindController
        | GameInputKind.GameInputKindKeyboard
        | GameInputKind.GameInputKindMouse
        | GameInputKind.GameInputKindSensors
        | GameInputKind.GameInputKindArcadeStick
        | GameInputKind.GameInputKindFlightStick
        | GameInputKind.GameInputKindGamepad
        | GameInputKind.GameInputKindRacingWheel;

    private readonly GameInputClient _client;
#if NET10_0_OR_GREATER
    private readonly System.Threading.Lock _cacheLock = new();
#else
    private readonly object _cacheLock = new();
#endif
    private readonly List<GameInputDevice> _devices = [];
    private readonly List<GameInputDeviceInfoSnapshot> _snapshots = [];
    private readonly Queue<GameInputDeviceManagerEvent> _events = new();
#if NET10_0_OR_GREATER
    private readonly System.Threading.Lock _pushLock = new();
#else
    private readonly object _pushLock = new();
#endif
    private readonly EventObservable<GameInputDeviceManagerEvent> _deviceChanges;
    private GameInputCallbackRegistration? _deviceEvents;
    private EventHandler<GameInputDeviceManagerEvent>? _deviceChangedHandlers;
    private int _pushSubscriberCount;
    private bool _manualDeviceEventsActive;
    private bool _disposed;

    private GameInputDeviceManager(GameInputClient client)
    {
        _client = client;
        _deviceChanges = new EventObservable<GameInputDeviceManagerEvent>(AddPushSubscriber, RemovePushSubscriber);
    }

    /// <summary>
    /// 建立裝置 manager，並擁有底層 <see cref="GameInputClient"/> 生命週期。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public static GameInputDeviceManager Create()
    {
        return new GameInputDeviceManager(GameInputClient.Create());
    }

    /// <summary>
    /// 已快取的裝置包裝快照複本。呼叫重新整理方法會釋放舊快取。
    /// </summary>
    public IReadOnlyList<GameInputDevice> Devices
    {
        get
        {
            ThrowIfDisposed();
            lock (_cacheLock)
            {
                return [.. _devices];
            }
        }
    }

    /// <summary>
    /// 不持有原生生命週期的裝置資訊快照複本。
    /// </summary>
    public IReadOnlyList<GameInputDeviceInfoSnapshot> DeviceSnapshots
    {
        get
        {
            ThrowIfDisposed();
            lock (_cacheLock)
            {
                return [.. _snapshots];
            }
        }
    }

    /// <summary>
    /// 以零配置方式存取目前已快取的裝置清單，避免 <see cref="Devices"/> 每次呼叫產生的陣列複本配置。
    /// </summary>
    /// <remarks>
    /// <paramref name="action"/> 在內部快取鎖持有期間執行；傳入的清單參考不得保存到呼叫範圍之外，
    /// 也不得在回呼內呼叫 <see cref="RefreshDevices()"/> 或其他會修改快取的方法，否則會造成死鎖。
    /// </remarks>
    /// <param name="action">以目前裝置快取為參數執行的回呼。</param>
    public void UseDevices(Action<IReadOnlyList<GameInputDevice>> action)
    {
        ThrowIfDisposed();
        lock (_cacheLock)
        {
            action(_devices);
        }
    }

    /// <summary>
    /// 以零配置方式存取目前已快取的裝置資訊快照清單，避免 <see cref="DeviceSnapshots"/> 每次呼叫產生的陣列複本配置。
    /// </summary>
    /// <remarks>
    /// <paramref name="action"/> 在內部快取鎖持有期間執行；傳入的清單參考不得保存到呼叫範圍之外，
    /// 也不得在回呼內呼叫 <see cref="RefreshDevices()"/> 或其他會修改快取的方法，否則會造成死鎖。
    /// </remarks>
    /// <param name="action">以目前裝置資訊快照快取為參數執行的回呼。</param>
    public void UseDeviceSnapshots(Action<IReadOnlyList<GameInputDeviceInfoSnapshot>> action)
    {
        ThrowIfDisposed();
        lock (_cacheLock)
        {
            action(_snapshots);
        }
    }

    /// <summary>
    /// 重新整理裝置快取。
    /// </summary>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="statusFilter">要篩選的裝置狀態。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public IReadOnlyList<GameInputDeviceInfoSnapshot> RefreshDevices(GameInputKind inputKind, GameInputDeviceStatus statusFilter = GameInputDeviceStatus.GameInputDeviceConnected)
    {
        ThrowIfDisposed();

        IReadOnlyList<GameInputDevice> devices = _client.EnumerateDevices(inputKind, statusFilter);
        List<GameInputDeviceInfoSnapshot> snapshots = new(devices.Count);
        foreach (GameInputDevice device in devices)
        {
            snapshots.Add(device.GetDeviceInfoSnapshot());
        }

        foreach (GameInputDevice previousDevice in ReplaceDevices(devices, snapshots))
        {
            previousDevice.Dispose();
        }

        return snapshots.AsReadOnly();
    }

    /// <summary>
    /// 使用預設 GameInput v3 主要輸入種類重新整理裝置快取。
    /// </summary>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public IReadOnlyList<GameInputDeviceInfoSnapshot> RefreshDevices()
    {
        return RefreshDevices(s_defaultInputKinds);
    }

    /// <summary>
    /// 以背景執行緒非同步重新整理裝置快取，避免呼叫端執行緒（例如 UI 執行緒）被原生列舉阻塞。
    /// </summary>
    /// <remarks>
    /// 這個方法只是把 <see cref="RefreshDevices(GameInputKind, GameInputDeviceStatus)"/> 包在
    /// <see cref="Task.Run(Action)"/> 中執行，不是等待原生事件的非同步方法；原生列舉呼叫本身仍是同步阻塞。
    /// </remarks>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="statusFilter">要篩選的裝置狀態。</param>
    /// <param name="cancellationToken">取消語彙。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public Task<IReadOnlyList<GameInputDeviceInfoSnapshot>> RefreshDevicesAsync(
        GameInputKind inputKind,
        GameInputDeviceStatus statusFilter = GameInputDeviceStatus.GameInputDeviceConnected,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Task.Run(() => RefreshDevices(inputKind, statusFilter), cancellationToken);
    }

    /// <summary>
    /// 使用預設 GameInput v3 主要輸入種類，以背景執行緒非同步重新整理裝置快取。
    /// </summary>
    /// <param name="cancellationToken">取消語彙。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public Task<IReadOnlyList<GameInputDeviceInfoSnapshot>> RefreshDevicesAsync(CancellationToken cancellationToken = default)
    {
        return RefreshDevicesAsync(s_defaultInputKinds, GameInputDeviceStatus.GameInputDeviceConnected, cancellationToken);
    }

    /// <summary>
    /// 非同步等待下一筆裝置狀態事件（例如插拔、連線狀態變化）。
    /// </summary>
    /// <remarks>
    /// 內部會註冊一次性原生回呼；完成或取消後會透過背景執行緒解除註冊，
    /// 不會在原生回呼執行緒中同步呼叫 <see cref="GameInputCallbackRegistration.Dispose"/>。
    /// </remarks>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="statusFilter">要篩選的裝置狀態。</param>
    /// <param name="cancellationToken">取消語彙。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public Task<GameInputDeviceManagerEvent> WaitForDeviceEventAsync(
        GameInputKind inputKind,
        GameInputDeviceStatus statusFilter = GameInputDeviceStatus.GameInputDeviceAnyStatus,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return _client.RunAwaitableCallback<GameInputDeviceManagerEvent>(
            onResult => _client.RegisterDeviceCallback(
                null,
                inputKind,
                statusFilter,
                GameInputEnumerationKind.GameInputAsyncEnumeration,
                (device, timestamp, currentStatus, previousStatus) =>
                {
                    GameInputDeviceInfoSnapshot snapshot = device.GetDeviceInfoSnapshot();
                    onResult(new GameInputDeviceManagerEvent(timestamp, currentStatus, previousStatus, snapshot));
                }),
            cancellationToken,
            nameof(GameInputDeviceManager));
    }

    /// <summary>
    /// 使用預設 GameInput v3 主要輸入種類，非同步等待下一筆裝置狀態事件。
    /// </summary>
    /// <param name="cancellationToken">取消語彙。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public Task<GameInputDeviceManagerEvent> WaitForDeviceEventAsync(CancellationToken cancellationToken = default)
    {
        return WaitForDeviceEventAsync(s_defaultInputKinds, GameInputDeviceStatus.GameInputDeviceAnyStatus, cancellationToken);
    }

    /// <summary>
    /// 裝置狀態變化事件（插拔、連線狀態變化）。
    /// </summary>
    /// <remarks>
    /// 訂閱／取消訂閱會自動管理裝置事件監看的啟動與停止，不需要另外手動呼叫 <see cref="StartDeviceEvents()"/>／
    /// <see cref="StopDeviceEvents"/>。若先手動呼叫 <see cref="StartDeviceEvents()"/> 再訂閱這個事件，
    /// 取消訂閱不會停止手動啟動的監看；若手動呼叫 <see cref="StopDeviceEvents"/> 停止監看，
    /// 目前仍有效的訂閱者要等到下一次新增訂閱者時才會恢復監看。
    /// 處理常式的例外不會拋出到原生回呼邊界，而是透過 <see cref="GameInputClient.UnhandledCallbackException"/> 觸發。
    /// </remarks>
    public event EventHandler<GameInputDeviceManagerEvent>? DeviceChanged
    {
        add
        {
            ThrowIfDisposed();
            if (value is null)
            {
                return;
            }

            lock (_pushLock)
            {
                _deviceChangedHandlers += value;
                try
                {
                    AddPushSubscriberLocked();
                }
                catch
                {
                    _deviceChangedHandlers -= value;
                    throw;
                }
            }
        }
        remove
        {
            if (value is null)
            {
                return;
            }

            lock (_pushLock)
            {
                _deviceChangedHandlers -= value;
                RemovePushSubscriberLocked();
            }
        }
    }

    /// <summary>
    /// 裝置狀態變化的 <see cref="IObservable{T}"/> 推送來源。
    /// </summary>
    /// <remarks>
    /// 訂閱／取消訂閱會自動管理裝置事件監看的啟動與停止，語意與 <see cref="DeviceChanged"/> 相同。
    /// <see cref="Dispose"/> 時會呼叫所有目前訂閱者的 <see cref="IObserver{T}.OnCompleted"/>；
    /// 在 <see cref="Dispose"/> 之後才呼叫 <see cref="IObservable{T}.Subscribe"/> 的新訂閱者，
    /// 會立即收到 <see cref="IObserver{T}.OnCompleted"/> 而不會收到任何 <see cref="IObserver{T}.OnNext"/>。
    /// </remarks>
    public IObservable<GameInputDeviceManagerEvent> DeviceChanges
    {
        get
        {
            ThrowIfDisposed();
            return _deviceChanges;
        }
    }

    /// <summary>
    /// 開始監看裝置狀態事件，事件會進入 manager 佇列。
    /// </summary>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="statusFilter">要篩選的裝置狀態。</param>
    public void StartDeviceEvents(GameInputKind inputKind, GameInputDeviceStatus statusFilter = GameInputDeviceStatus.GameInputDeviceAnyStatus)
    {
        ThrowIfDisposed();

        lock (_pushLock)
        {
            StopDeviceEvents();

            _deviceEvents = _client.RegisterDeviceCallback(
                null,
                inputKind,
                statusFilter,
                GameInputEnumerationKind.GameInputAsyncEnumeration,
                EnqueueDeviceEvent);
            _manualDeviceEventsActive = true;
        }
    }

    /// <summary>
    /// 使用預設 GameInput v3 主要輸入種類開始監看裝置狀態事件。
    /// </summary>
    public void StartDeviceEvents()
    {
        StartDeviceEvents(s_defaultInputKinds);
    }

    /// <summary>
    /// 停止監看裝置狀態事件。
    /// </summary>
    public void StopDeviceEvents()
    {
        Exception? failure;
        lock (_pushLock)
        {
            failure = _deviceEvents?.DisposeSafely();
            _deviceEvents = null;
            _manualDeviceEventsActive = false;
        }

        if (failure is not null)
        {
            throw failure;
        }
    }

    /// <summary>
    /// 嘗試從事件佇列取出下一筆裝置事件。
    /// </summary>
    /// <param name="managerEvent">參數 managerEvent。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public bool TryDequeueEvent(out GameInputDeviceManagerEvent managerEvent)
    {
        ThrowIfDisposed();
        lock (_events)
        {
            if (_events.Count == 0)
            {
                managerEvent = default!;
                return false;
            }

            managerEvent = _events.Dequeue();
            return true;
        }
    }

    /// <summary>
    /// 取得目前指定種類的 reading。
    /// </summary>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GameInputReading? GetCurrentReading(GameInputKind inputKind, GameInputDevice? device = null)
    {
        ThrowIfDisposed();
        return _client.GetCurrentReading(inputKind, device);
    }

    /// <summary>
    /// 取得目前 gamepad 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public GamepadReadingSnapshot? GetCurrentGamepad(GameInputDevice? device = null)
    {
        ThrowIfDisposed();
        return _client.GetCurrentGamepad(device);
    }

    /// <summary>
    /// 取得目前 keyboard 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public KeyboardReadingSnapshot? GetCurrentKeyboard(GameInputDevice? device = null)
    {
        ThrowIfDisposed();
        return _client.GetCurrentKeyboard(device);
    }

    /// <summary>
    /// 取得目前 mouse 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public MouseReadingSnapshot? GetCurrentMouse(GameInputDevice? device = null)
    {
        ThrowIfDisposed();
        return _client.GetCurrentMouse(device);
    }

    /// <summary>
    /// 取得目前 sensors 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public SensorsReadingSnapshot? GetCurrentSensors(GameInputDevice? device = null)
    {
        ThrowIfDisposed();
        return _client.GetCurrentSensors(device);
    }

    /// <summary>
    /// 取得目前一般 controller 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public ControllerReadingSnapshot? GetCurrentController(GameInputDevice? device = null)
    {
        ThrowIfDisposed();
        return _client.GetCurrentController(device);
    }

    /// <summary>
    /// 取得目前 arcade stick 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public ArcadeStickReadingSnapshot? GetCurrentArcadeStick(GameInputDevice? device = null)
    {
        ThrowIfDisposed();
        return _client.GetCurrentArcadeStick(device);
    }

    /// <summary>
    /// 取得目前 flight stick 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public FlightStickReadingSnapshot? GetCurrentFlightStick(GameInputDevice? device = null)
    {
        ThrowIfDisposed();
        return _client.GetCurrentFlightStick(device);
    }

    /// <summary>
    /// 取得目前 racing wheel 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public RacingWheelReadingSnapshot? GetCurrentRacingWheel(GameInputDevice? device = null)
    {
        ThrowIfDisposed();
        return _client.GetCurrentRacingWheel(device);
    }

    /// <summary>
    /// 取得目前 raw device report 快照。
    /// </summary>
    /// <param name="device">選用的 GameInput 裝置篩選。</param>
    /// <returns>操作完成後的查詢或建立結果。</returns>
    public RawDeviceReportSnapshot? GetCurrentRawReport(GameInputDevice? device = null)
    {
        ThrowIfDisposed();
        return _client.GetCurrentRawReport(device);
    }

    /// <summary>
    /// 從目前快取中尋找第一個支援指定輸入種類的裝置。
    /// </summary>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="device">成功時接收裝置包裝。</param>
    /// <param name="snapshot">成功時接收對應的裝置資訊快照。</param>
    /// <returns>若目前快取中有符合條件的裝置，傳回 true；否則傳回 false。</returns>
    public bool TryGetFirstDevice(GameInputKind inputKind, out GameInputDevice? device, out GameInputDeviceInfoSnapshot? snapshot)
    {
        ThrowIfDisposed();
        lock (_cacheLock)
        {
            int index = FindFirstDeviceIndex(_snapshots, inputKind);
            return TryGetCachedDevice(index, out device, out snapshot);
        }
    }

    /// <summary>
    /// 從目前快取中尋找第一個支援 gamepad 的裝置。
    /// </summary>
    /// <param name="device">成功時接收裝置包裝。</param>
    /// <param name="snapshot">成功時接收對應的裝置資訊快照。</param>
    /// <returns>若目前快取中有支援 gamepad 的裝置，傳回 true；否則傳回 false。</returns>
    public bool TryGetFirstGamepad(out GameInputDevice? device, out GameInputDeviceInfoSnapshot? snapshot)
    {
        return TryGetFirstDevice(GameInputKind.GameInputKindGamepad, out device, out snapshot);
    }

    /// <summary>
    /// 從目前快取中尋找第一個支援 keyboard 的裝置。
    /// </summary>
    /// <param name="device">成功時接收裝置包裝。</param>
    /// <param name="snapshot">成功時接收對應的裝置資訊快照。</param>
    /// <returns>若目前快取中有支援 keyboard 的裝置，傳回 true；否則傳回 false。</returns>
    public bool TryGetFirstKeyboard(out GameInputDevice? device, out GameInputDeviceInfoSnapshot? snapshot)
    {
        return TryGetFirstDevice(GameInputKind.GameInputKindKeyboard, out device, out snapshot);
    }

    /// <summary>
    /// 從目前快取中尋找第一個支援 mouse 的裝置。
    /// </summary>
    /// <param name="device">成功時接收裝置包裝。</param>
    /// <param name="snapshot">成功時接收對應的裝置資訊快照。</param>
    /// <returns>若目前快取中有支援 mouse 的裝置，傳回 true；否則傳回 false。</returns>
    public bool TryGetFirstMouse(out GameInputDevice? device, out GameInputDeviceInfoSnapshot? snapshot)
    {
        return TryGetFirstDevice(GameInputKind.GameInputKindMouse, out device, out snapshot);
    }

    /// <summary>
    /// 從目前快取中尋找第一個宣告支援 rumble motor 的裝置。
    /// </summary>
    /// <param name="device">成功時接收裝置包裝。</param>
    /// <param name="snapshot">成功時接收對應的裝置資訊快照。</param>
    /// <returns>若目前快取中有支援 rumble 的裝置，傳回 true；否則傳回 false。</returns>
    public bool TryGetFirstRumbleDevice(out GameInputDevice? device, out GameInputDeviceInfoSnapshot? snapshot)
    {
        ThrowIfDisposed();
        lock (_cacheLock)
        {
            int index = FindFirstRumbleDeviceIndex(_snapshots);
            return TryGetCachedDevice(index, out device, out snapshot);
        }
    }

    /// <summary>
    /// 停止裝置事件並釋放 manager 持有的 GameInput 資源。
    /// </summary>
    /// <exception cref="Exception">
    /// 裝置事件註冊釋放時發生非預期例外；此時其餘資源仍會完成釋放。
    /// </exception>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Exception? stopDeviceEventsFailure = null;
        try
        {
            StopDeviceEvents();
        }
        catch (Exception ex)
        {
            stopDeviceEventsFailure = ex;
        }

        foreach (GameInputDevice previousDevice in ReplaceDevices([], []))
        {
            previousDevice.Dispose();
        }

        _deviceChanges.Complete();
        _client.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);

        if (stopDeviceEventsFailure is not null)
        {
            throw stopDeviceEventsFailure;
        }
    }

    private void EnqueueDeviceEvent(GameInputDevice device, ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus)
    {
        GameInputDeviceInfoSnapshot snapshot = device.GetDeviceInfoSnapshot();
        GameInputDeviceManagerEvent managerEvent = new(timestamp, currentStatus, previousStatus, snapshot);
        lock (_events)
        {
            _events.Enqueue(managerEvent);
        }

        RaiseDeviceChanged(managerEvent);
        _deviceChanges.OnNext(managerEvent);
    }

    private void RaiseDeviceChanged(GameInputDeviceManagerEvent managerEvent)
    {
        EventHandler<GameInputDeviceManagerEvent>? handlers = _deviceChangedHandlers;
        if (handlers is null)
        {
            return;
        }

        try
        {
            handlers(this, managerEvent);
        }
        catch (Exception ex)
        {
            GameInputClient.RaiseUnhandledCallbackException(ex);
        }
    }

    private void AddPushSubscriber()
    {
        lock (_pushLock)
        {
            AddPushSubscriberLocked();
        }
    }

    private void RemovePushSubscriber()
    {
        lock (_pushLock)
        {
            RemovePushSubscriberLocked();
        }
    }

    /// <summary>
    /// 假設呼叫端已持有 <see cref="_pushLock"/>。
    /// </summary>
    private void AddPushSubscriberLocked()
    {
        _pushSubscriberCount++;
        if (_deviceEvents is null)
        {
            try
            {
                _deviceEvents = _client.RegisterDeviceCallback(
                    null,
                    s_defaultInputKinds,
                    GameInputDeviceStatus.GameInputDeviceAnyStatus,
                    GameInputEnumerationKind.GameInputAsyncEnumeration,
                    EnqueueDeviceEvent);
            }
            catch
            {
                _pushSubscriberCount--;
                throw;
            }
        }
    }

    /// <summary>
    /// 假設呼叫端已持有 <see cref="_pushLock"/>。
    /// </summary>
    private void RemovePushSubscriberLocked()
    {
        _pushSubscriberCount = Math.Max(0, _pushSubscriberCount - 1);
        if (_pushSubscriberCount != 0 || _manualDeviceEventsActive)
        {
            return;
        }

        if (GameInputCallbackThread.IsExecutingCallback)
        {
            // 這次取消訂閱是從原生回呼執行緒觸發（例如 handler 內自我取消訂閱），
            // 這個時候同步呼叫 StopDeviceEvents() 會因為仍在回呼執行緒中而丟出
            // InvalidOperationException；改到背景執行緒延後處理，並在執行時重新檢查
            // 目前狀態（延後期間可能又有新訂閱者加入）。
            // 這裡刻意用 fire-and-forget（不等待背景執行緒完成），因為呼叫端只是取消訂閱、
            // 不需要確定裝置事件監看已經真正停止才能回傳；跟 GameInputCallbackRegistration.DisposeSafely()
            // 需要同步等待（因為呼叫端接下來會釋放 registration 依賴的原生資源）是不同的取捨，
            // 兩者故意沒有合併成同一個共用的「延後到背景執行緒」輔助方法。
            ThreadPool.QueueUserWorkItem(static state => ((GameInputDeviceManager)state!).StopDeviceEventsIfStillUnwanted(), this);
        }
        else
        {
            StopDeviceEvents();
        }
    }

    private void StopDeviceEventsIfStillUnwanted()
    {
        lock (_pushLock)
        {
            if (_pushSubscriberCount == 0 && !_manualDeviceEventsActive)
            {
                StopDeviceEvents();
            }
        }
    }

    private List<GameInputDevice> ReplaceDevices(IReadOnlyList<GameInputDevice> devices, IReadOnlyList<GameInputDeviceInfoSnapshot> snapshots)
    {
        lock (_cacheLock)
        {
            List<GameInputDevice> previousDevices = [.. _devices];

            _devices.Clear();
            _devices.AddRange(devices);
            _snapshots.Clear();
            _snapshots.AddRange(snapshots);

            return previousDevices;
        }
    }

    private bool TryGetCachedDevice(int index, out GameInputDevice? device, out GameInputDeviceInfoSnapshot? snapshot)
    {
        if (index < 0 || index >= _devices.Count || index >= _snapshots.Count)
        {
            device = null;
            snapshot = null;
            return false;
        }

        device = _devices[index];
        snapshot = _snapshots[index];
        return true;
    }

    internal static int FindFirstDeviceIndex(IReadOnlyList<GameInputDeviceInfoSnapshot> snapshots, GameInputKind inputKind)
    {
        return FindFirstIndex(snapshots, inputKind, static (snapshot, kind) => (snapshot.SupportedInput & kind) == kind);
    }

    internal static int FindFirstRumbleDeviceIndex(IReadOnlyList<GameInputDeviceInfoSnapshot> snapshots)
    {
        return FindFirstIndex(
            snapshots,
            GameInputRumbleMotors.GameInputRumbleNone,
            static (snapshot, none) => snapshot.SupportedRumbleMotors != none);
    }

    private static int FindFirstIndex<TState>(
        IReadOnlyList<GameInputDeviceInfoSnapshot> snapshots,
        TState state,
        Func<GameInputDeviceInfoSnapshot, TState, bool> predicate)
    {
        for (int index = 0; index < snapshots.Count; index++)
        {
            if (predicate(snapshots[index], state))
            {
                return index;
            }
        }

        return -1;
    }

    private void ThrowIfDisposed()
    {
#if NET10_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(GameInputDeviceManager));
        }
#endif
    }
}

/// <summary>
/// GameInput manager 佇列中的裝置事件。
/// </summary>
/// <param name="timestamp">GameInput 時間戳記。</param>
/// <param name="currentStatus">目前裝置狀態。</param>
/// <param name="previousStatus">先前裝置狀態。</param>
/// <param name="device">選用的 GameInput 裝置篩選。</param>
public sealed class GameInputDeviceManagerEvent(
    ulong timestamp,
    GameInputDeviceStatus currentStatus,
    GameInputDeviceStatus previousStatus,
    GameInputDeviceInfoSnapshot device)
{
    /// <summary>
    /// 事件對應的 GameInput 時間戳記。
    /// </summary>
    public ulong Timestamp { get; } = timestamp;

    /// <summary>
    /// 事件發生後的裝置狀態。
    /// </summary>
    public GameInputDeviceStatus CurrentStatus { get; } = currentStatus;

    /// <summary>
    /// 事件發生前的裝置狀態。
    /// </summary>
    public GameInputDeviceStatus PreviousStatus { get; } = previousStatus;

    /// <summary>
    /// 事件對應的裝置資訊快照。
    /// </summary>
    public GameInputDeviceInfoSnapshot Device { get; } = device;
}
