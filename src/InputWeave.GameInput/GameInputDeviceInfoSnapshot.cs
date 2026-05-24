using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput
{
    /// <summary>
    /// 不持有原生指標的 GameInput 裝置資訊快照。
    /// </summary>
    public sealed class GameInputDeviceInfoSnapshot
    {
        internal GameInputDeviceInfoSnapshot(
            GameInputDeviceInfo native,
            string? displayName,
            string? pnpPath,
            GameInputKeyboardInfo? keyboard,
            GameInputMouseInfo? mouse,
            GameInputSensorsInfo? sensors,
            GameInputControllerInfoSnapshot? controller,
            GameInputArcadeStickInfo? arcadeStick,
            GameInputFlightStickInfo? flightStick,
            GameInputGamepadInfo? gamepad,
            GameInputRacingWheelInfo? racingWheel,
            IReadOnlyList<GameInputForceFeedbackMotorInfo> forceFeedbackMotors,
            IReadOnlyList<GameInputRawDeviceReportInfo> inputReports,
            IReadOnlyList<GameInputRawDeviceReportInfo> outputReports)
        {
            Native = native;
            DisplayName = displayName;
            PnpPath = pnpPath;
            Keyboard = keyboard;
            Mouse = mouse;
            Sensors = sensors;
            Controller = controller;
            ArcadeStick = arcadeStick;
            FlightStick = flightStick;
            Gamepad = gamepad;
            RacingWheel = racingWheel;
            ForceFeedbackMotors = forceFeedbackMotors;
            InputReports = inputReports;
            OutputReports = outputReports;
        }

        /// <summary>
        /// 原生固定欄位快照；所有指標欄位只供診斷，不應在快照外解參考。
        /// </summary>
        public GameInputDeviceInfo Native { get; }

        public ushort VendorId
        {
            get
            {
                return Native.VendorId;
            }
        }

        public ushort ProductId
        {
            get
            {
                return Native.ProductId;
            }
        }

        public ushort RevisionNumber
        {
            get
            {
                return Native.RevisionNumber;
            }
        }

        public GameInputUsage Usage
        {
            get
            {
                return Native.Usage;
            }
        }

        public GameInputDeviceFamily DeviceFamily
        {
            get
            {
                return Native.DeviceFamily;
            }
        }

        public GameInputKind SupportedInput
        {
            get
            {
                return Native.SupportedInput;
            }
        }

        public GameInputRumbleMotors SupportedRumbleMotors
        {
            get
            {
                return Native.SupportedRumbleMotors;
            }
        }

        public GameInputSystemButtons SupportedSystemButtons
        {
            get
            {
                return Native.SupportedSystemButtons;
            }
        }

        public Guid ContainerId
        {
            get
            {
                return Native.ContainerId;
            }
        }

        public string? DisplayName { get; }

        public string? PnpPath { get; }

        public GameInputKeyboardInfo? Keyboard { get; }

        public GameInputMouseInfo? Mouse { get; }

        public GameInputSensorsInfo? Sensors { get; }

        public GameInputControllerInfoSnapshot? Controller { get; }

        public GameInputArcadeStickInfo? ArcadeStick { get; }

        public GameInputFlightStickInfo? FlightStick { get; }

        public GameInputGamepadInfo? Gamepad { get; }

        public GameInputRacingWheelInfo? RacingWheel { get; }

        public IReadOnlyList<GameInputForceFeedbackMotorInfo> ForceFeedbackMotors { get; }

        public IReadOnlyList<GameInputRawDeviceReportInfo> InputReports { get; }

        public IReadOnlyList<GameInputRawDeviceReportInfo> OutputReports { get; }

        internal static GameInputDeviceInfoSnapshot FromNative(GameInputDeviceInfo native)
        {
            return new GameInputDeviceInfoSnapshot(
                native,
                PtrToAnsiString(native.DisplayName),
                PtrToAnsiString(native.PnpPath),
                PtrToNullableStructure<GameInputKeyboardInfo>(native.KeyboardInfo),
                PtrToNullableStructure<GameInputMouseInfo>(native.MouseInfo),
                PtrToNullableStructure<GameInputSensorsInfo>(native.SensorsInfo),
                GameInputControllerInfoSnapshot.FromPointer(native.ControllerInfo),
                PtrToNullableStructure<GameInputArcadeStickInfo>(native.ArcadeStickInfo),
                PtrToNullableStructure<GameInputFlightStickInfo>(native.FlightStickInfo),
                PtrToNullableStructure<GameInputGamepadInfo>(native.GamepadInfo),
                PtrToNullableStructure<GameInputRacingWheelInfo>(native.RacingWheelInfo),
                ReadArray<GameInputForceFeedbackMotorInfo>(native.ForceFeedbackMotorInfo, native.ForceFeedbackMotorCount),
                ReadArray<GameInputRawDeviceReportInfo>(native.InputReportInfo, native.InputReportCount),
                ReadArray<GameInputRawDeviceReportInfo>(native.OutputReportInfo, native.OutputReportCount));
        }

        private static string? PtrToAnsiString(IntPtr pointer)
        {
            return pointer == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(pointer);
        }

        private static T? PtrToNullableStructure<T>(IntPtr pointer)
            where T : struct
        {
            return pointer == IntPtr.Zero ? null : Marshal.PtrToStructure<T>(pointer);
        }

        internal static IReadOnlyList<T> ReadArray<T>(IntPtr pointer, uint count)
            where T : struct
        {
            if (pointer == IntPtr.Zero || count == 0)
            {
                return Array.Empty<T>();
            }

            int itemSize = Marshal.SizeOf<T>();
            T[] values = new T[checked((int)count)];
            for (int index = 0; index < values.Length; index++)
            {
                values[index] = Marshal.PtrToStructure<T>(IntPtr.Add(pointer, checked(index * itemSize)));
            }

            return values;
        }
    }

    /// <summary>
    /// 不持有原生指標的 controller 子資訊快照。
    /// </summary>
    public sealed class GameInputControllerInfoSnapshot
    {
        private GameInputControllerInfoSnapshot(
            GameInputControllerInfo native,
            IReadOnlyList<GameInputLabel> axisLabels,
            IReadOnlyList<GameInputLabel> buttonLabels,
            IReadOnlyList<GameInputControllerSwitchInfoSnapshot> switches)
        {
            Native = native;
            AxisLabels = axisLabels;
            ButtonLabels = buttonLabels;
            Switches = switches;
        }

        public GameInputControllerInfo Native { get; }

        public IReadOnlyList<GameInputLabel> AxisLabels { get; }

        public IReadOnlyList<GameInputLabel> ButtonLabels { get; }

        public IReadOnlyList<GameInputControllerSwitchInfoSnapshot> Switches { get; }

        internal static GameInputControllerInfoSnapshot? FromPointer(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            GameInputControllerInfo native = Marshal.PtrToStructure<GameInputControllerInfo>(pointer);
            return new GameInputControllerInfoSnapshot(
                native,
                GameInputDeviceInfoSnapshot.ReadArray<GameInputLabel>(native.ControllerAxisLabels, native.ControllerAxisCount),
                GameInputDeviceInfoSnapshot.ReadArray<GameInputLabel>(native.ControllerButtonLabels, native.ControllerButtonCount),
                ReadSwitchInfo(native.ControllerSwitchInfo, native.ControllerSwitchCount));
        }

        private static IReadOnlyList<GameInputControllerSwitchInfoSnapshot> ReadSwitchInfo(IntPtr pointer, uint count)
        {
            if (pointer == IntPtr.Zero || count == 0)
            {
                return Array.Empty<GameInputControllerSwitchInfoSnapshot>();
            }

            int itemSize = Marshal.SizeOf<GameInputControllerSwitchInfo>();
            GameInputControllerSwitchInfoSnapshot[] values = new GameInputControllerSwitchInfoSnapshot[checked((int)count)];
            for (int index = 0; index < values.Length; index++)
            {
                GameInputControllerSwitchInfo native = Marshal.PtrToStructure<GameInputControllerSwitchInfo>(IntPtr.Add(pointer, checked(index * itemSize)));
                values[index] = GameInputControllerSwitchInfoSnapshot.FromNative(native);
            }

            return values;
        }
    }

    /// <summary>
    /// 不持有 fixed buffer 的 controller switch 子資訊快照。
    /// </summary>
    public sealed class GameInputControllerSwitchInfoSnapshot
    {
        private GameInputControllerSwitchInfoSnapshot(IReadOnlyList<GameInputLabel> labels, GameInputSwitchKind kind)
        {
            Labels = labels;
            Kind = kind;
        }

        public IReadOnlyList<GameInputLabel> Labels { get; }

        public GameInputSwitchKind Kind { get; }

        internal static unsafe GameInputControllerSwitchInfoSnapshot FromNative(GameInputControllerSwitchInfo native)
        {
            GameInputLabel[] labels = new GameInputLabel[GameInputConstants.MaxSwitchStates];
            for (int index = 0; index < labels.Length; index++)
            {
                labels[index] = (GameInputLabel)native.Labels[index];
            }

            return new GameInputControllerSwitchInfoSnapshot(labels, native.Kind);
        }
    }

    /// <summary>
    /// 不持有原生 haptic 緩衝區的觸覺資訊快照。
    /// </summary>
    public sealed class GameInputHapticInfoSnapshot
    {
        internal GameInputHapticInfoSnapshot(string audioEndpointId, IReadOnlyList<Guid> locations)
        {
            AudioEndpointId = audioEndpointId;
            Locations = locations;
        }

        public string AudioEndpointId { get; }

        public IReadOnlyList<Guid> Locations { get; }

        internal static GameInputHapticInfoSnapshot FromNative(GameInputHapticInfo native)
        {
            Guid[] locations = Array.Empty<Guid>();
            if (native.Locations is not null && native.LocationCount > 0)
            {
                int count = Math.Min(checked((int)native.LocationCount), native.Locations.Length);
                locations = new Guid[count];
                Array.Copy(native.Locations, locations, count);
            }

            return new GameInputHapticInfoSnapshot(native.AudioEndpointId ?? string.Empty, locations);
        }
    }
}
