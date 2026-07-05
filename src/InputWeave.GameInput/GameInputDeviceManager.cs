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
    private GameInputCallbackRegistration? _deviceEvents;
    private bool _disposed;

    private GameInputDeviceManager(GameInputClient client)
    {
        _client = client;
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
    /// 開始監看裝置狀態事件，事件會進入 manager 佇列。
    /// </summary>
    /// <param name="inputKind">要查詢或篩選的 GameInput 輸入種類。</param>
    /// <param name="statusFilter">要篩選的裝置狀態。</param>
    public void StartDeviceEvents(GameInputKind inputKind, GameInputDeviceStatus statusFilter = GameInputDeviceStatus.GameInputDeviceAnyStatus)
    {
        ThrowIfDisposed();
        StopDeviceEvents();

        _deviceEvents = _client.RegisterDeviceCallback(
            null,
            inputKind,
            statusFilter,
            GameInputEnumerationKind.GameInputAsyncEnumeration,
            EnqueueDeviceEvent);
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
        _deviceEvents?.Dispose();
        _deviceEvents = null;
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
    /// <exception cref="InvalidOperationException">
    /// 裝置事件註冊因為在自身的原生回呼執行緒中被同步釋放而無法停止；此時其餘資源仍會完成釋放。
    /// </exception>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        InvalidOperationException? stopDeviceEventsFailure = null;
        try
        {
            StopDeviceEvents();
        }
        catch (InvalidOperationException ex)
        {
            stopDeviceEventsFailure = ex;
        }

        foreach (GameInputDevice previousDevice in ReplaceDevices([], []))
        {
            previousDevice.Dispose();
        }

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
        lock (_events)
        {
            _events.Enqueue(new GameInputDeviceManagerEvent(timestamp, currentStatus, previousStatus, snapshot));
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
