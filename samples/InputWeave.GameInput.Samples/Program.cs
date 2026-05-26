using System.Diagnostics;
using InputWeave.GameInput;
using InputWeave.GameInput.Interop;

Console.OutputEncoding = System.Text.Encoding.UTF8;

bool enableRumble = args.Any(static argument => string.Equals(argument, "--rumble", StringComparison.OrdinalIgnoreCase));
string? unknownArgument = args.FirstOrDefault(static argument =>
    !string.Equals(argument, "--rumble", StringComparison.OrdinalIgnoreCase) &&
    !string.Equals(argument, "--help", StringComparison.OrdinalIgnoreCase) &&
    !string.Equals(argument, "-h", StringComparison.OrdinalIgnoreCase));

if (args.Any(static argument => string.Equals(argument, "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(argument, "-h", StringComparison.OrdinalIgnoreCase)))
{
    PrintUsage();
    return;
}

if (unknownArgument is not null)
{
    Console.WriteLine($"未知參數：{unknownArgument}");
    PrintUsage();
    return;
}

try
{
    PrintHeader(enableRumble);

    using GameInputDeviceManager manager = GameInputDeviceManager.Create();
    IReadOnlyList<GameInputDeviceInfoSnapshot> snapshots = manager.RefreshDevices();

    PrintDevices(manager, snapshots);
    PrintGamepadSnapshot(manager);
    PrintOtherInputSnapshots(manager);
    DemonstrateDispatcherAndCallback();
    DemonstrateHapticsAndRumble(manager, enableRumble);
}
catch (DllNotFoundException ex)
{
    Console.WriteLine("GameInput.dll 無法載入。請確認目標機器已安裝支援的 Windows GameInput runtime 或隨應用程式安裝 GameInput redist。");
    Console.WriteLine(ex.Message);
}
catch (EntryPointNotFoundException ex)
{
    Console.WriteLine("目前載入的 GameInput.dll 不含必要進入點。請確認 GameInput runtime 版本符合 Microsoft.GameInput 3.4.218。");
    Console.WriteLine(ex.Message);
}
catch (GameInputException ex) when (ex.IsNotFound)
{
    Console.WriteLine($"找不到符合條件的 GameInput 裝置或讀取資料：0x{ex.HResult:X8} {ex.Message}");
}
catch (GameInputException ex)
{
    Console.WriteLine($"GameInput 呼叫失敗：0x{ex.HResult:X8} {ex.Message}");
}

static void PrintUsage()
{
    Console.WriteLine("InputWeave.GameInput quickstart sample");
    Console.WriteLine();
    Console.WriteLine("用法：");
    Console.WriteLine("  dotnet run --project samples/InputWeave.GameInput.Samples");
    Console.WriteLine("  dotnet run --project samples/InputWeave.GameInput.Samples -- --rumble");
    Console.WriteLine();
    Console.WriteLine("預設只做唯讀列舉、polling 與 callback 示範。傳入 --rumble 才會短暫觸發支援裝置的低強度震動。");
}

static void PrintHeader(bool enableRumble)
{
    WriteSection("啟動");
    Console.WriteLine("InputWeave.GameInput v0.0.1 quickstart sample");
    Console.WriteLine(enableRumble
        ? "Rumble 測試：已啟用，僅會對支援裝置短暫輸出低強度震動。"
        : "Rumble 測試：未啟用，預設不會觸發任何硬體震動。");
}

static void PrintDevices(GameInputDeviceManager manager, IReadOnlyList<GameInputDeviceInfoSnapshot> snapshots)
{
    WriteSection("裝置列舉");
    Console.WriteLine($"已列舉裝置：{snapshots.Count}");

    if (snapshots.Count == 0)
    {
        Console.WriteLine("沒有找到已連線的 GameInput 裝置。接上控制器、鍵盤或滑鼠後再執行可看到裝置資訊。");
        return;
    }

    for (int index = 0; index < snapshots.Count; index++)
    {
        GameInputDeviceInfoSnapshot snapshot = snapshots[index];
        GameInputDeviceStatus status = index < manager.Devices.Count ? manager.Devices[index].Status : 0;
        Console.WriteLine(
            $"{index + 1}. {GetDisplayName(snapshot)} / {status} / {snapshot.SupportedInput} / Rumble:{snapshot.SupportedRumbleMotors} / VID:{snapshot.VendorId:X4} PID:{snapshot.ProductId:X4}");
    }
}

static void PrintGamepadSnapshot(GameInputDeviceManager manager)
{
    WriteSection("Gamepad polling");

    if (!manager.TryGetFirstGamepad(out GameInputDevice? device, out GameInputDeviceInfoSnapshot? snapshot))
    {
        Console.WriteLine("目前沒有支援 gamepad input kind 的裝置；略過 gamepad snapshot。");
        return;
    }

    GamepadReadingSnapshot? gamepad = manager.GetCurrentGamepad(device);
    if (gamepad is null)
    {
        Console.WriteLine($"已找到 gamepad 裝置「{GetDisplayName(snapshot)}」，但目前沒有可讀取的 gamepad snapshot。");
        Console.WriteLine("如果應用程式需要背景輸入，請在實際程式中設定合適的 focus policy。");
        return;
    }

    GameInputGamepadState state = gamepad.State;
    Console.WriteLine($"裝置：{GetDisplayName(snapshot)}");
    Console.WriteLine($"Timestamp：{gamepad.Timestamp}");
    Console.WriteLine($"Buttons：{state.Buttons}");
    Console.WriteLine($"Triggers：L={state.LeftTrigger:F2}, R={state.RightTrigger:F2}");
    Console.WriteLine($"Thumbsticks：L=({state.LeftThumbstickX:F2}, {state.LeftThumbstickY:F2}), R=({state.RightThumbstickX:F2}, {state.RightThumbstickY:F2})");
}

static void PrintOtherInputSnapshots(GameInputDeviceManager manager)
{
    WriteSection("其他輸入快照");

    KeyboardReadingSnapshot? keyboard = manager.GetCurrentKeyboard();
    MouseReadingSnapshot? mouse = manager.GetCurrentMouse();
    SensorsReadingSnapshot? sensors = manager.GetCurrentSensors();

    bool printed = false;
    if (keyboard is not null)
    {
        Console.WriteLine($"Keyboard：目前作用中按鍵 {keyboard.Keys.Count} 個。");
        printed = true;
    }

    if (mouse is not null)
    {
        Console.WriteLine($"Mouse：Buttons={mouse.State.Buttons}, Position=({mouse.State.PositionX}, {mouse.State.PositionY})。");
        printed = true;
    }

    if (sensors is not null)
    {
        Console.WriteLine($"Sensors：OrientationW={sensors.State.OrientationW:F2}, Heading={sensors.State.HeadingInDegreesFromMagneticNorth:F2}。");
        printed = true;
    }

    if (!printed)
    {
        Console.WriteLine("目前沒有 keyboard、mouse 或 sensors snapshot；沒有對應輸入或暫時沒有資料時這是正常結果。");
    }
}

static void DemonstrateDispatcherAndCallback()
{
    WriteSection("Dispatcher 與 callback");

    using GameInputClient client = GameInputClient.Create();
    using GameInputDispatcher dispatcher = client.CreateDispatcher();
    using GameInputDispatcherWaitHandle waitHandle = dispatcher.OpenSafeWaitHandle();

    int deviceCallbackCount = 0;
    int readingCallbackCount = 0;
    using GameInputCallbackRegistration deviceRegistration = client.RegisterDeviceCallback(
        null,
        GameInputKind.GameInputKindGamepad,
        GameInputDeviceStatus.GameInputDeviceAnyStatus,
        GameInputEnumerationKind.GameInputAsyncEnumeration,
        (device, timestamp, current, previous) =>
        {
            _ = Interlocked.Increment(ref deviceCallbackCount);
            Console.WriteLine($"Device callback：{timestamp} {previous} -> {current} {device.GetDisplayName() ?? "(未命名裝置)"}");
        });
    using GameInputCallbackRegistration readingRegistration = client.RegisterReadingCallback(
        null,
        GameInputKind.GameInputKindGamepad,
        reading =>
        {
            if (!reading.TryGetGamepadSnapshot(out GamepadReadingSnapshot? snapshot))
            {
                return;
            }

            int count = Interlocked.Increment(ref readingCallbackCount);
            if (count <= 3)
            {
                Console.WriteLine($"Reading callback：{snapshot!.Timestamp} Buttons={snapshot.State.Buttons}");
            }
        });

    Console.WriteLine(waitHandle.IsInvalid
        ? "Dispatcher safe wait handle 無效；本次僅示範直接 Dispatch。"
        : "Dispatcher safe wait handle 已建立。");
    Console.WriteLine($"Device callback token：{deviceRegistration.Token}");
    Console.WriteLine($"Reading callback token：{readingRegistration.Token}");

    Stopwatch stopwatch = Stopwatch.StartNew();
    while (stopwatch.ElapsedMilliseconds < 250)
    {
        _ = dispatcher.Dispatch(1_000);
        Thread.Sleep(25);
    }

    if (deviceCallbackCount == 0)
    {
        Console.WriteLine("這次沒有收到裝置事件；沒有插拔或狀態變化時這是正常結果。");
    }

    if (readingCallbackCount == 0)
    {
        Console.WriteLine("這次沒有收到 gamepad reading callback；沒有輸入事件或沒有 gamepad 時這是正常結果。");
    }
}

static void DemonstrateHapticsAndRumble(GameInputDeviceManager manager, bool enableRumble)
{
    WriteSection("Haptics 與 rumble");

    if (!manager.TryGetFirstRumbleDevice(out GameInputDevice? device, out GameInputDeviceInfoSnapshot? snapshot) &&
        !manager.TryGetFirstGamepad(out device, out snapshot))
    {
        Console.WriteLine("沒有可用裝置；略過 haptics 與 rumble。");
        return;
    }

    Console.WriteLine($"選用裝置：{GetDisplayName(snapshot)}");

    Console.WriteLine(device!.TryGetHapticInfoSnapshot(out GameInputHapticInfoSnapshot? haptic)
        ? $"Haptic endpoint：{haptic!.AudioEndpointId} / Locations：{haptic.Locations.Count}"
        : "此裝置沒有 haptic 資訊。");

    if (!enableRumble)
    {
        Console.WriteLine("未傳入 --rumble；略過震動測試。");
        return;
    }

    if (snapshot!.SupportedRumbleMotors == GameInputRumbleMotors.GameInputRumbleNone)
    {
        Console.WriteLine("此裝置未宣告支援 rumble motor；略過震動測試。");
        return;
    }

    try
    {
        using GameInputRumbleScope? rumble = device.StartRumbleScope(
            lowFrequency: 0.15f,
            highFrequency: 0.15f,
            leftTrigger: 0.10f,
            rightTrigger: 0.10f);
        if (rumble is null)
        {
            Console.WriteLine("裝置不支援這組 rumble 輸出；略過震動測試。");
            return;
        }

        Thread.Sleep(250);
        Console.WriteLine($"已對支援馬達輸出短暫低強度震動：{snapshot.SupportedRumbleMotors}");
    }
    catch (GameInputException ex)
    {
        Console.WriteLine($"震動測試失敗：0x{ex.HResult:X8} {ex.Message}");
    }
}

static string GetDisplayName(GameInputDeviceInfoSnapshot? snapshot)
{
    return string.IsNullOrWhiteSpace(snapshot?.DisplayName) ? "(未命名裝置)" : snapshot.DisplayName;
}

static void WriteSection(string title)
{
    Console.WriteLine();
    Console.WriteLine($"== {title} ==");
}
