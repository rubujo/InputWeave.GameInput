using InputWeave.GameInput;
using InputWeave.GameInput.Interop;

Console.OutputEncoding = System.Text.Encoding.UTF8;

try
{
    using GameInputDeviceManager manager = GameInputDeviceManager.Create();
    IReadOnlyList<GameInputDeviceInfoSnapshot> devices = manager.RefreshDevices();
    Console.WriteLine($"InputWeave.GameInput v0.0.1 範例");
    Console.WriteLine($"已列舉裝置：{devices.Count}");

    foreach (GameInputDeviceInfoSnapshot device in devices)
    {
        Console.WriteLine($"- {device.DisplayName ?? "(未命名裝置)"} / {device.SupportedInput} / VID:{device.VendorId:X4} PID:{device.ProductId:X4}");
    }

    GamepadReadingSnapshot? gamepad = manager.GetCurrentGamepad();
    if (gamepad is not null)
    {
        Console.WriteLine($"Gamepad timestamp：{gamepad.Timestamp}");
        Console.WriteLine($"Gamepad buttons：{gamepad.State.Buttons}");
    }
    else
    {
        Console.WriteLine("目前沒有可讀取的 gamepad 快照。");
    }

    using GameInputClient client = GameInputClient.Create();
    using GameInputDispatcher dispatcher = client.CreateDispatcher();
    using GameInputDispatcherWaitHandle waitHandle = dispatcher.OpenSafeWaitHandle();
    Console.WriteLine($"Dispatcher wait handle：0x{waitHandle.DangerousGetHandle().ToInt64():X}");

    using GameInputCallbackRegistration registration = client.RegisterDeviceCallback(
        null,
        GameInputKind.GameInputKindGamepad,
        GameInputDeviceStatus.GameInputDeviceAnyStatus,
        GameInputEnumerationKind.GameInputAsyncEnumeration,
        static (device, timestamp, current, previous) =>
        {
            Console.WriteLine($"Device callback：{timestamp} {previous} -> {current} {device.GetDisplayName()}");
        });

    _ = dispatcher.Dispatch(0);
    Console.WriteLine($"Callback token：{registration.Token}");

    if (devices.Count > 0)
    {
        using GameInputDevice firstDevice = client.FindDeviceFromPlatformString(devices[0].PnpPath ?? string.Empty);
        GameInputHapticInfoSnapshot? haptic = firstDevice.GetHapticInfoSnapshot();
        Console.WriteLine(haptic is null ? "此裝置沒有 haptic 資訊。" : $"Haptic endpoint：{haptic.AudioEndpointId}");

        GameInputRumbleParams rumble = GameInputForceFeedback.Rumble(lowFrequency: 0.2f, highFrequency: 0.2f);
        firstDevice.SetRumbleState(rumble);
        firstDevice.ClearRumbleState();
    }
}
catch (ArgumentException)
{
    Console.WriteLine("範例找不到可用的 PnP 路徑；請接上支援 GameInput 的裝置後再試。");
}
catch (DllNotFoundException ex)
{
    Console.WriteLine($"此機器未載入 GameInput.dll：{ex.Message}");
}
catch (EntryPointNotFoundException ex)
{
    Console.WriteLine($"此 GameInput.dll 不含必要進入點：{ex.Message}");
}
catch (GameInputException ex)
{
    Console.WriteLine($"GameInput 呼叫失敗：0x{ex.HResult:X8} {ex.Message}");
}
