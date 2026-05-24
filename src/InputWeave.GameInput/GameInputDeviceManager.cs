using System;
using System.Collections.Generic;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput
{
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
        public static GameInputDeviceManager Create()
        {
            return new GameInputDeviceManager(GameInputClient.Create());
        }

        /// <summary>
        /// 已快取的裝置包裝。呼叫重新整理方法會釋放舊快取。
        /// </summary>
        public IReadOnlyList<GameInputDevice> Devices
        {
            get
            {
                ThrowIfDisposed();
                return _devices.AsReadOnly();
            }
        }

        /// <summary>
        /// 不持有原生生命週期的裝置資訊快照。
        /// </summary>
        public IReadOnlyList<GameInputDeviceInfoSnapshot> DeviceSnapshots
        {
            get
            {
                ThrowIfDisposed();
                return _snapshots.AsReadOnly();
            }
        }

        /// <summary>
        /// 重新整理裝置快取。
        /// </summary>
        public IReadOnlyList<GameInputDeviceInfoSnapshot> RefreshDevices(GameInputKind inputKind, GameInputDeviceStatus statusFilter = GameInputDeviceStatus.GameInputDeviceConnected)
        {
            ThrowIfDisposed();
            ClearDevices();

            IReadOnlyList<GameInputDevice> devices = _client.EnumerateDevices(inputKind, statusFilter);
            foreach (GameInputDevice device in devices)
            {
                _devices.Add(device);
                _snapshots.Add(device.GetDeviceInfoSnapshot());
            }

            return _snapshots.AsReadOnly();
        }

        /// <summary>
        /// 使用預設 GameInput v3 主要輸入種類重新整理裝置快取。
        /// </summary>
        public IReadOnlyList<GameInputDeviceInfoSnapshot> RefreshDevices()
        {
            return RefreshDevices(s_defaultInputKinds);
        }

        /// <summary>
        /// 開始監看裝置狀態事件，事件會進入 manager 佇列。
        /// </summary>
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
        public GameInputReading? GetCurrentReading(GameInputKind inputKind, GameInputDevice? device = null)
        {
            ThrowIfDisposed();
            return _client.GetCurrentReading(inputKind, device);
        }

        /// <summary>
        /// 取得目前 gamepad 快照。
        /// </summary>
        public GamepadReadingSnapshot? GetCurrentGamepad(GameInputDevice? device = null)
        {
            ThrowIfDisposed();
            return _client.GetCurrentGamepad(device);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            StopDeviceEvents();
            ClearDevices();
            _client.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void EnqueueDeviceEvent(GameInputDevice device, ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus)
        {
            GameInputDeviceInfoSnapshot snapshot = device.GetDeviceInfoSnapshot();
            lock (_events)
            {
                _events.Enqueue(new GameInputDeviceManagerEvent(timestamp, currentStatus, previousStatus, snapshot));
            }
        }

        private void ClearDevices()
        {
            foreach (GameInputDevice device in _devices)
            {
                device.Dispose();
            }

            _devices.Clear();
            _snapshots.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GameInputDeviceManager));
            }
        }
    }

    /// <summary>
    /// GameInput manager 佇列中的裝置事件。
    /// </summary>
    public sealed class GameInputDeviceManagerEvent
    {
        public GameInputDeviceManagerEvent(ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus, GameInputDeviceInfoSnapshot device)
        {
            Timestamp = timestamp;
            CurrentStatus = currentStatus;
            PreviousStatus = previousStatus;
            Device = device;
        }

        public ulong Timestamp { get; }

        public GameInputDeviceStatus CurrentStatus { get; }

        public GameInputDeviceStatus PreviousStatus { get; }

        public GameInputDeviceInfoSnapshot Device { get; }
    }
}
