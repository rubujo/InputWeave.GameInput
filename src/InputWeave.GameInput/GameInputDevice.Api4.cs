using System.Text;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

public sealed partial class GameInputDevice
{
    /// <summary>Gets VendorId. 取得 VendorId。</summary>
    /// <returns>The VendorId value. VendorId 值。</returns>
    public ushort GetVendorId()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetVendorId();
    }

    /// <summary>Gets ProductId. 取得 ProductId。</summary>
    /// <returns>The ProductId value. ProductId 值。</returns>
    public ushort GetProductId()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetProductId();
    }

    /// <summary>Gets RevisionNumber. 取得 RevisionNumber。</summary>
    /// <returns>The RevisionNumber value. RevisionNumber 值。</returns>
    public ushort GetRevisionNumber()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetRevisionNumber();
    }

    /// <summary>Gets DeviceFamily. 取得 DeviceFamily。</summary>
    /// <returns>The DeviceFamily value. DeviceFamily 值。</returns>
    public GameInputDeviceFamily GetDeviceFamily()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetDeviceFamily();
    }

    /// <summary>Gets SupportedInput. 取得 SupportedInput。</summary>
    /// <returns>The SupportedInput value. SupportedInput 值。</returns>
    public GameInputKind GetSupportedInput()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetSupportedInput();
    }

    /// <summary>Gets SupportedRumbleMotors. 取得 SupportedRumbleMotors。</summary>
    /// <returns>The SupportedRumbleMotors value. SupportedRumbleMotors 值。</returns>
    public GameInputRumbleMotors GetSupportedRumbleMotors()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetSupportedRumbleMotors();
    }

    /// <summary>Gets SupportedSystemButtons. 取得 SupportedSystemButtons。</summary>
    /// <returns>The SupportedSystemButtons value. SupportedSystemButtons 值。</returns>
    public GameInputSystemButtons GetSupportedSystemButtons()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetSupportedSystemButtons();
    }

    /// <summary>Gets SupportedLightingGroups. 取得 SupportedLightingGroups。</summary>
    /// <returns>The SupportedLightingGroups value. SupportedLightingGroups 值。</returns>
    public GameInputLightingGroups GetSupportedLightingGroups()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetSupportedLightingGroups();
    }

    /// <summary>Gets Usage. 取得 Usage。</summary>
    /// <returns>The Usage value. Usage 值。</returns>
    public GameInputUsage GetUsage()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        call.Native.GetUsage(out GameInputUsage usage);
        return usage;
    }

    /// <summary>Gets HardwareVersion. 取得 HardwareVersion。</summary>
    /// <returns>The HardwareVersion value. HardwareVersion 值。</returns>
    public GameInputVersion GetHardwareVersion()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        call.Native.GetHardwareVersion(out GameInputVersion version);
        return version;
    }

    /// <summary>Gets FirmwareVersion. 取得 FirmwareVersion。</summary>
    /// <returns>The FirmwareVersion value. FirmwareVersion 值。</returns>
    public GameInputVersion GetFirmwareVersion()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        call.Native.GetFirmwareVersion(out GameInputVersion version);
        return version;
    }

    /// <summary>Gets DeviceId. 取得 DeviceId。</summary>
    /// <returns>The DeviceId value. DeviceId 值。</returns>
    public AppLocalDeviceId GetDeviceId()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        call.Native.GetDeviceId(out AppLocalDeviceId deviceId);
        return deviceId;
    }

    /// <summary>Gets DeviceRootId. 取得 DeviceRootId。</summary>
    /// <returns>The DeviceRootId value. DeviceRootId 值。</returns>
    public AppLocalDeviceId GetDeviceRootId()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        call.Native.GetDeviceRootId(out AppLocalDeviceId deviceRootId);
        return deviceRootId;
    }

    /// <summary>Gets ContainerId. 取得 ContainerId。</summary>
    /// <returns>The ContainerId value. ContainerId 值。</returns>
    public Guid GetContainerId()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        call.Native.GetContainerId(out Guid containerId);
        return containerId;
    }

    /// <summary>Gets optional Keyboard capabilities. 取得選用的 Keyboard 能力。</summary>
    /// <returns>The information, or null when unsupported. 資訊；不支援時為 null。</returns>
    public GameInputKeyboardInfo? GetKeyboardInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetKeyboardInfo(out GameInputKeyboardInfo info) ? info : null;
    }

    /// <summary>Gets optional Mouse capabilities. 取得選用的 Mouse 能力。</summary>
    /// <returns>The information, or null when unsupported. 資訊；不支援時為 null。</returns>
    public GameInputMouseInfo? GetMouseInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetMouseInfo(out GameInputMouseInfo info) ? info : null;
    }

    /// <summary>Gets optional Sensors capabilities. 取得選用的 Sensors 能力。</summary>
    /// <returns>The information, or null when unsupported. 資訊；不支援時為 null。</returns>
    public GameInputSensorsInfo? GetSensorsInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetSensorsInfo(out GameInputSensorsInfo info) ? info : null;
    }

    /// <summary>Gets optional ArcadeStick capabilities. 取得選用的 ArcadeStick 能力。</summary>
    /// <returns>The information, or null when unsupported. 資訊；不支援時為 null。</returns>
    public GameInputArcadeStickInfo? GetArcadeStickInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetArcadeStickInfo(out GameInputArcadeStickInfo info) ? info : null;
    }

    /// <summary>Gets optional FlightStick capabilities. 取得選用的 FlightStick 能力。</summary>
    /// <returns>The information, or null when unsupported. 資訊；不支援時為 null。</returns>
    public GameInputFlightStickInfo? GetFlightStickInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetFlightStickInfo(out GameInputFlightStickInfo info) ? info : null;
    }

    /// <summary>Gets optional Gamepad capabilities. 取得選用的 Gamepad 能力。</summary>
    /// <returns>The information, or null when unsupported. 資訊；不支援時為 null。</returns>
    public GameInputGamepadInfo? GetGamepadInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetGamepadInfo(out GameInputGamepadInfo info) ? info : null;
    }

    /// <summary>Gets optional RacingWheel capabilities. 取得選用的 RacingWheel 能力。</summary>
    /// <returns>The information, or null when unsupported. 資訊；不支援時為 null。</returns>
    public GameInputRacingWheelInfo? GetRacingWheelInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetRacingWheelInfo(out GameInputRacingWheelInfo info) ? info : null;
    }

    /// <summary>Gets the ControllerAxis count. 取得 ControllerAxis 數量。</summary>
    /// <returns>The element count. 元素數量。</returns>
    public uint GetControllerAxisCount()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetControllerAxisCount();
    }

    /// <summary>Copies all ControllerAxis Labels into managed memory. 將所有 ControllerAxis Labels 複製至受控記憶體。</summary>
    /// <returns>An independent array. 獨立陣列。</returns>
    public GameInputLabel[] GetControllerAxisLabels()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return ReadArray<GameInputLabel>(call.Native.GetControllerAxisCount, call.Native.GetControllerAxisLabels);
    }

    /// <summary>Gets the ControllerButton count. 取得 ControllerButton 數量。</summary>
    /// <returns>The element count. 元素數量。</returns>
    public uint GetControllerButtonCount()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetControllerButtonCount();
    }

    /// <summary>Copies all ControllerButton Labels into managed memory. 將所有 ControllerButton Labels 複製至受控記憶體。</summary>
    /// <returns>An independent array. 獨立陣列。</returns>
    public GameInputLabel[] GetControllerButtonLabels()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return ReadArray<GameInputLabel>(call.Native.GetControllerButtonCount, call.Native.GetControllerButtonLabels);
    }

    /// <summary>Gets the ControllerSwitch count. 取得 ControllerSwitch 數量。</summary>
    /// <returns>The element count. 元素數量。</returns>
    public uint GetControllerSwitchCount()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetControllerSwitchCount();
    }

    /// <summary>Copies all ControllerSwitch Info into managed memory. 將所有 ControllerSwitch Info 複製至受控記憶體。</summary>
    /// <returns>An independent array. 獨立陣列。</returns>
    public GameInputControllerSwitchInfo[] GetControllerSwitchInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return ReadArray<GameInputControllerSwitchInfo>(call.Native.GetControllerSwitchCount, call.Native.GetControllerSwitchInfo);
    }

    /// <summary>Gets the ForceFeedbackMotor count. 取得 ForceFeedbackMotor 數量。</summary>
    /// <returns>The element count. 元素數量。</returns>
    public uint GetForceFeedbackMotorCount()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetForceFeedbackMotorCount();
    }

    /// <summary>Copies all ForceFeedbackMotor Info into managed memory. 將所有 ForceFeedbackMotor Info 複製至受控記憶體。</summary>
    /// <returns>An independent array. 獨立陣列。</returns>
    public GameInputForceFeedbackMotorInfo[] GetForceFeedbackMotorInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return ReadArray<GameInputForceFeedbackMotorInfo>(call.Native.GetForceFeedbackMotorCount, call.Native.GetForceFeedbackMotorInfo);
    }

    /// <summary>Gets the InputReport count. 取得 InputReport 數量。</summary>
    /// <returns>The element count. 元素數量。</returns>
    public uint GetInputReportCount()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetInputReportCount();
    }

    /// <summary>Copies all InputReport Info into managed memory. 將所有 InputReport Info 複製至受控記憶體。</summary>
    /// <returns>An independent array. 獨立陣列。</returns>
    public GameInputRawDeviceReportInfo[] GetInputReportInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return ReadArray<GameInputRawDeviceReportInfo>(call.Native.GetInputReportCount, call.Native.GetInputReportInfo);
    }

    /// <summary>Gets the OutputReport count. 取得 OutputReport 數量。</summary>
    /// <returns>The element count. 元素數量。</returns>
    public uint GetOutputReportCount()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetOutputReportCount();
    }

    /// <summary>Copies all OutputReport Info into managed memory. 將所有 OutputReport Info 複製至受控記憶體。</summary>
    /// <returns>An independent array. 獨立陣列。</returns>
    public GameInputRawDeviceReportInfo[] GetOutputReportInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return ReadArray<GameInputRawDeviceReportInfo>(call.Native.GetOutputReportCount, call.Native.GetOutputReportInfo);
    }

    /// <summary>Gets the FeatureReport count. 取得 FeatureReport 數量。</summary>
    /// <returns>The element count. 元素數量。</returns>
    public uint GetFeatureReportCount()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetFeatureReportCount();
    }

    /// <summary>Copies all FeatureReport Info into managed memory. 將所有 FeatureReport Info 複製至受控記憶體。</summary>
    /// <returns>An independent array. 獨立陣列。</returns>
    public GameInputRawDeviceReportInfo[] GetFeatureReportInfo()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return ReadArray<GameInputRawDeviceReportInfo>(call.Native.GetFeatureReportCount, call.Native.GetFeatureReportInfo);
    }

    /// <summary>Queries the XInput device index. 查詢 XInput 裝置索引。</summary>
    /// <param name="index">Receives the index. 接收索引。</param>
    /// <returns>True when an index is available. 有可用索引時為 true。</returns>
    public bool TryGetXInputDeviceIndex(out uint index)
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetXInputDeviceIndex(out index);
    }

    /// <summary>Checks whether the device is connected. 檢查裝置是否已連線。</summary>
    /// <returns>True when connected. 已連線時為 true。</returns>
    public bool IsConnected()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.IsConnected();
    }

    /// <summary>Gets the lighting group size. 取得燈光群組大小。</summary>
    /// <param name="group">The lighting group. 燈光群組。</param>
    /// <returns>The number of lights. 燈光數量。</returns>
    public uint GetSupportedLightingGroupSize(GameInputLightingGroups group)
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetSupportedLightingGroupSize(group);
    }

    /// <summary>Sets a pending RGB lighting color; apply it with ApplyLightingColors. 設定待套用的 RGB 燈光色彩；以 ApplyLightingColors 套用。</summary>
    /// <param name="color">The RGB color. RGB 色彩。</param>
    /// <param name="group">The lighting group. 燈光群組。</param>
    /// <param name="groupOrderIndex">The light index, or uint.MaxValue for all lights. 燈光索引；uint.MaxValue 代表全部燈光。</param>
    public void SetLightingColor(GameInputColor color, GameInputLightingGroups group, uint groupOrderIndex = uint.MaxValue)
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        call.Native.SetLightingColor(color, group, groupOrderIndex);
    }

    /// <summary>Applies pending lighting colors. 套用待處理燈光色彩。</summary>
    public void ApplyLightingColors()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        call.Native.ApplyLightingColors();
    }

    /// <summary>Restores system default lighting. 還原系統預設燈光。</summary>
    public void ApplyLightingSystemDefault()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        call.Native.ApplyLightingSystemDefault();
    }

    /// <summary>Gets the number of touch surfaces. 取得觸控表面數量。</summary>
    /// <returns>The surface count. 表面數量。</returns>
    public uint GetTouchSurfaceCount()
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        return call.Native.GetTouchSurfaceCount();
    }

    /// <summary>Gets touch surface capabilities. 取得觸控表面能力。</summary>
    /// <param name="surfaceIndex">The surface index. 表面索引。</param>
    /// <returns>The surface capabilities. 表面能力。</returns>
    public GameInputTouchSurfaceInfo GetTouchSurfaceInfo(uint surfaceIndex)
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        GameInputException.ThrowIfFailed(call.Native.GetTouchSurfaceInfo(surfaceIndex, out GameInputTouchSurfaceInfo info));
        return info;
    }

    /// <summary>Gets a raw feature report. 取得原始功能報告。</summary>
    /// <param name="reportId">The report identifier. 報告識別碼。</param>
    /// <returns>An owned report that must be disposed. 必須釋放的自有報告。</returns>
    public GameInputRawDeviceReport GetRawDeviceFeature(uint reportId)
    {
        using ComLease<IGameInputDevice> call = EnterNative();
        GameInputException.ThrowIfFailed(call.Native.GetRawDeviceFeature(reportId, out IGameInputRawDeviceReport? report));
        return report is { } value ? new GameInputRawDeviceReport(value) : throw new GameInputException(GameInputHResult.ObjectNoLongerExists);
    }

    /// <summary>Writes a raw feature report. 寫入原始功能報告。</summary>
    /// <param name="report">The feature report. 功能報告。</param>
    public void SetRawDeviceFeature(GameInputRawDeviceReport report)
    {
        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }
        using ComLease<IGameInputDevice> call = EnterNative();
        using ComLease<IGameInputRawDeviceReport> reportLease = report.EnterNative();
        GameInputException.ThrowIfFailed(call.Native.SetRawDeviceFeature(reportLease.Native));
    }

    private static GameInputDeviceIdentity ReadFixedInfo(IGameInputDevice native)
    {
        native.GetUsage(out GameInputUsage usage);
        native.GetHardwareVersion(out GameInputVersion hardware);
        native.GetFirmwareVersion(out GameInputVersion firmware);
        native.GetDeviceId(out AppLocalDeviceId deviceId);
        native.GetDeviceRootId(out AppLocalDeviceId rootId);
        native.GetContainerId(out Guid containerId);
        return new GameInputDeviceIdentity
        {
            VendorId = native.GetVendorId(),
            ProductId = native.GetProductId(),
            RevisionNumber = native.GetRevisionNumber(),
            Usage = usage,
            HardwareVersion = hardware,
            FirmwareVersion = firmware,
            DeviceId = deviceId,
            DeviceRootId = rootId,
            DeviceFamily = native.GetDeviceFamily(),
            SupportedInput = native.GetSupportedInput(),
            SupportedRumbleMotors = native.GetSupportedRumbleMotors(),
            SupportedSystemButtons = native.GetSupportedSystemButtons(),
            ContainerId = containerId
        };
    }

    private static GameInputDeviceInfoSnapshot ReadSnapshot(IGameInputDevice native)
    {
        GameInputDeviceIdentity fixedInfo = ReadFixedInfo(native);
        GameInputLabel[] axes = ReadArray<GameInputLabel>(native.GetControllerAxisCount, native.GetControllerAxisLabels);
        GameInputLabel[] buttons = ReadArray<GameInputLabel>(native.GetControllerButtonCount, native.GetControllerButtonLabels);
        GameInputControllerSwitchInfoSnapshot[] switches = ReadArray<GameInputControllerSwitchInfo>(native.GetControllerSwitchCount, native.GetControllerSwitchInfo).Select(GameInputControllerSwitchInfoSnapshot.FromNative).ToArray();
        return new GameInputDeviceInfoSnapshot(
            fixedInfo, ReadString(native.GetDisplayName), ReadString(native.GetPnpPath),
            native.GetKeyboardInfo(out GameInputKeyboardInfo keyboard) ? keyboard : null,
            native.GetMouseInfo(out GameInputMouseInfo mouse) ? mouse : null,
            native.GetSensorsInfo(out GameInputSensorsInfo sensors) ? sensors : null,
            new GameInputControllerInfoSnapshot(axes, buttons, switches),
            native.GetArcadeStickInfo(out GameInputArcadeStickInfo arcade) ? arcade : null,
            native.GetFlightStickInfo(out GameInputFlightStickInfo flight) ? flight : null,
            native.GetGamepadInfo(out GameInputGamepadInfo gamepad) ? gamepad : null,
            native.GetRacingWheelInfo(out GameInputRacingWheelInfo wheel) ? wheel : null,
            ReadArray<GameInputForceFeedbackMotorInfo>(native.GetForceFeedbackMotorCount, native.GetForceFeedbackMotorInfo),
            ReadArray<GameInputRawDeviceReportInfo>(native.GetInputReportCount, native.GetInputReportInfo),
            ReadArray<GameInputRawDeviceReportInfo>(native.GetOutputReportCount, native.GetOutputReportInfo))
        {
            SupportedLightingGroups = native.GetSupportedLightingGroups(),
            TouchSurfaces = ReadTouchSurfaces(native),
            XInputDeviceIndex = native.GetXInputDeviceIndex(out uint index) ? index : null,
            FeatureReports = ReadArray<GameInputRawDeviceReportInfo>(native.GetFeatureReportCount, native.GetFeatureReportInfo)
        };
    }

    private static GameInputTouchSurfaceInfo[] ReadTouchSurfaces(IGameInputDevice native)
    {
        GameInputTouchSurfaceInfo[] surfaces = new GameInputTouchSurfaceInfo[NativeSizeGuard.EnsureCount(native.GetTouchSurfaceCount(), NativeSizeGuard.MaxElementCount, "觸控表面數量")];
        for (int index = 0; index < surfaces.Length; index++)
        {
            GameInputException.ThrowIfFailed(native.GetTouchSurfaceInfo((uint)index, out surfaces[index]));
        }
        return surfaces;
    }

    internal static unsafe T[] ReadArray<T>(Func<uint> count, Func<uint, IntPtr, uint> copy) where T : unmanaged
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            T[] values = new T[NativeSizeGuard.EnsureCount(count(), NativeSizeGuard.MaxElementCount, "裝置資訊元素數量")];
            uint written;
            fixed (T* buffer = values)
            {
                written = copy((uint)values.Length, (IntPtr)buffer);
            }
            NativeSizeGuard.EnsureCount(written, NativeSizeGuard.MaxElementCount, "裝置資訊元素數量");
            if (written > values.Length)
            {
                continue;
            }
            if (written != values.Length)
            {
                Array.Resize(ref values, (int)written);
            }
            return values;
        }
        throw new InvalidOperationException("裝置資訊在複製期間持續增長，無法取得一致陣列。");
    }

    internal static unsafe string? ReadString(Func<uint, IntPtr, uint> copy)
    {
        uint size = copy(0, IntPtr.Zero);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (size == 0)
            {
                return null;
            }
            byte[] bytes = new byte[NativeSizeGuard.EnsureCount(size, NativeSizeGuard.MaxUtf8StringByteLength, "裝置字串位元組數")];
            uint written;
            fixed (byte* buffer = bytes)
            {
                written = copy((uint)bytes.Length, (IntPtr)buffer);
            }
            if (written > bytes.Length)
            {
                size = written;
                continue;
            }
            int length = Array.IndexOf(bytes, (byte)0, 0, (int)written);
            return Encoding.UTF8.GetString(bytes, 0, length < 0 ? (int)written : length);
        }
        throw new InvalidOperationException("裝置字串在複製期間持續增長，無法取得一致字串。");
    }
}
