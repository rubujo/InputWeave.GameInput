using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>Fixed device details copied from API 4 getters, without native pointers. 從 API 4 getter 複製的裝置固定資訊，不含原生指標。</summary>
public readonly record struct GameInputDeviceIdentity
{
    /// <summary>The VendorId value. 供應商識別碼。</summary>
    public ushort VendorId { get; init; }

    /// <summary>The ProductId value. 產品識別碼。</summary>
    public ushort ProductId { get; init; }

    /// <summary>The RevisionNumber value. 修訂編號。</summary>
    public ushort RevisionNumber { get; init; }

    /// <summary>The Usage value. HID 使用方式。</summary>
    public GameInputUsage Usage { get; init; }

    /// <summary>The HardwareVersion value. 硬體版本。</summary>
    public GameInputVersion HardwareVersion { get; init; }

    /// <summary>The FirmwareVersion value. 韌體版本。</summary>
    public GameInputVersion FirmwareVersion { get; init; }

    /// <summary>The DeviceId value. 裝置識別碼。</summary>
    public AppLocalDeviceId DeviceId { get; init; }

    /// <summary>The DeviceRootId value. 根裝置識別碼。</summary>
    public AppLocalDeviceId DeviceRootId { get; init; }

    /// <summary>The DeviceFamily value. 裝置家族。</summary>
    public GameInputDeviceFamily DeviceFamily { get; init; }

    /// <summary>The SupportedInput value. 支援的輸入種類。</summary>
    public GameInputKind SupportedInput { get; init; }

    /// <summary>The SupportedRumbleMotors value. 支援的震動馬達。</summary>
    public GameInputRumbleMotors SupportedRumbleMotors { get; init; }

    /// <summary>The SupportedSystemButtons value. 支援的系統按鈕。</summary>
    public GameInputSystemButtons SupportedSystemButtons { get; init; }

    /// <summary>The ContainerId value. 容器識別碼。</summary>
    public Guid ContainerId { get; init; }

}
