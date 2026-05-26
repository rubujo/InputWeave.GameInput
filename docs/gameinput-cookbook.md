# InputWeave.GameInput 常見情境指南

本文件提供可以直接放進主控台應用程式或方法中的常見整合片段。正式應用程式仍應依自己的遊戲迴圈、執行緒模型與安裝流程調整錯誤處理。

## 遊戲控制器輪詢迴圈

這個片段會列舉裝置、以能力選出第一個支援 Gamepad 的裝置，然後用簡單迴圈讀取目前快照。沒有遊戲控制器或暫時沒有讀取資料時，程式會清楚略過。

```csharp
using System;
using System.Threading;
using InputWeave.GameInput;

using GameInputDeviceManager manager = GameInputDeviceManager.Create();
manager.RefreshDevices();

if (!manager.TryGetFirstGamepad(out GameInputDevice? gamepadDevice, out GameInputDeviceInfoSnapshot? gamepadInfo))
{
    Console.WriteLine("目前沒有支援 Gamepad 的 GameInput 裝置。");
    return;
}

Console.WriteLine($"使用裝置：{GetDisplayName(gamepadInfo)}");

for (int frame = 0; frame < 600; frame++)
{
    GamepadReadingSnapshot? gamepad = manager.GetCurrentGamepad(gamepadDevice);
    if (gamepad is null)
    {
        Console.WriteLine("目前沒有可讀取的 Gamepad 快照。");
    }
    else
    {
        GameInputGamepadState state = gamepad.State;
        Console.WriteLine(
            $"Buttons:{state.Buttons} LT:{state.LeftTrigger:F2} RT:{state.RightTrigger:F2} " +
            $"LX:{state.LeftThumbstickX:F2} LY:{state.LeftThumbstickY:F2}");
    }

    Thread.Sleep(16);
}

static string GetDisplayName(GameInputDeviceInfoSnapshot? snapshot)
{
    return string.IsNullOrWhiteSpace(snapshot?.DisplayName) ? "(未命名裝置)" : snapshot.DisplayName;
}
```

## 多輸入輪詢

高階 current reading API 會在沒有資料時回傳 `null`，不需要把 `GameInputReading` 或 COM 物件保存到下一個 frame。應用程式可以依自己的 frame loop 只取需要的 snapshot。

```csharp
using System;
using InputWeave.GameInput;

using GameInputDeviceManager manager = GameInputDeviceManager.Create();

GamepadReadingSnapshot? gamepad = manager.GetCurrentGamepad();
KeyboardReadingSnapshot? keyboard = manager.GetCurrentKeyboard();
MouseReadingSnapshot? mouse = manager.GetCurrentMouse();
SensorsReadingSnapshot? sensors = manager.GetCurrentSensors();

if (gamepad is not null)
{
    Console.WriteLine($"Gamepad buttons: {gamepad.State.Buttons}");
}

if (keyboard is not null)
{
    Console.WriteLine($"Keys: {keyboard.Keys.Count}");
}

if (mouse is not null)
{
    Console.WriteLine($"Mouse: {mouse.State.PositionX}, {mouse.State.PositionY}");
}

if (sensors is not null)
{
    Console.WriteLine($"Orientation W: {sensors.State.OrientationW:F2}");
}
```

## 裝置熱插拔與回呼

這個片段示範建立分派器、Safe Wait Handle 與裝置回呼。沒有插拔或狀態變化時，不收到回呼是正常結果；回呼內的 `GameInputDevice` 只在回呼執行期間有效，不要存起來跨回呼使用。

```csharp
using System;
using System.Diagnostics;
using System.Threading;
using InputWeave.GameInput;
using InputWeave.GameInput.Interop;

using GameInputClient client = GameInputClient.Create();
using GameInputDispatcher dispatcher = client.CreateDispatcher();
using GameInputDispatcherWaitHandle waitHandle = dispatcher.OpenSafeWaitHandle();

using GameInputCallbackRegistration registration = client.RegisterDeviceCallback(
    device: null,
    inputKind: GameInputKind.GameInputKindGamepad,
    statusFilter: GameInputDeviceStatus.GameInputDeviceAnyStatus,
    enumerationKind: GameInputEnumerationKind.GameInputAsyncEnumeration,
    handler: OnDeviceChanged);

Console.WriteLine(waitHandle.IsInvalid
    ? "Dispatcher Safe Wait Handle 無效；改用直接 Dispatch。"
    : "Dispatcher Safe Wait Handle 已建立。");

Stopwatch watch = Stopwatch.StartNew();
while (watch.Elapsed < TimeSpan.FromSeconds(10))
{
    _ = dispatcher.Dispatch(1_000);
    Thread.Sleep(16);
}

static void OnDeviceChanged(
    GameInputDevice device,
    ulong timestamp,
    GameInputDeviceStatus currentStatus,
    GameInputDeviceStatus previousStatus)
{
    Console.WriteLine(
        $"{timestamp}: {previousStatus} -> {currentStatus} / {device.GetDisplayName() ?? "(未命名裝置)"}");
}
```

## Reading callback 轉快照

Callback handler 內收到的 `GameInputReading` 只應在 handler 執行期間使用。若資料要交給其他執行緒或稍後處理，請立即轉成 snapshot。

```csharp
using System;
using InputWeave.GameInput;
using InputWeave.GameInput.Interop;

using GameInputClient client = GameInputClient.Create();
using GameInputCallbackRegistration registration = client.RegisterReadingCallback(
    device: null,
    inputKind: GameInputKind.GameInputKindGamepad,
    handler: OnReading);

static void OnReading(GameInputReading reading)
{
    if (reading.TryGetGamepadSnapshot(out GamepadReadingSnapshot? gamepad))
    {
        Console.WriteLine($"Gamepad snapshot: {gamepad.Timestamp} / {gamepad.State.Buttons}");
    }
}
```

## 明確啟用震動

震動功能應該由使用者明確啟用。這個片段預設不震動，只有傳入 `--rumble` 時才對支援裝置輸出短暫低強度震動，並用 `finally` 清除狀態。

```csharp
using System;
using System.Linq;
using System.Threading;
using InputWeave.GameInput;

bool enableRumble = args.Any(static argument => string.Equals(argument, "--rumble", StringComparison.OrdinalIgnoreCase));
if (!enableRumble)
{
    Console.WriteLine("未傳入 --rumble；預設不觸發任何硬體震動。");
    return;
}

using GameInputDeviceManager manager = GameInputDeviceManager.Create();
manager.RefreshDevices();

if (!manager.TryGetFirstRumbleDevice(out GameInputDevice? device, out GameInputDeviceInfoSnapshot? snapshot))
{
    Console.WriteLine("目前沒有宣告支援震動馬達的 GameInput 裝置。");
    return;
}

using GameInputRumbleScope? rumble = device!.StartRumbleScope(
    lowFrequency: 0.15f,
    highFrequency: 0.15f,
    leftTrigger: 0.10f,
    rightTrigger: 0.10f);
if (rumble is null)
{
    Console.WriteLine("裝置宣告的 rumble 能力不支援這組輸出。");
    return;
}

Thread.Sleep(250);
Console.WriteLine($"已短暫觸發低強度 Rumble：{snapshot.SupportedRumbleMotors}");
```

## Force Feedback 明確啟用

Force Feedback 應先檢查裝置能力，再建立 effect。`TryCreateForceFeedbackEffect` 會在不支援時回傳 `false`，避免把能力不足當成一般例外處理。

```csharp
using System;
using InputWeave.GameInput;
using InputWeave.GameInput.Interop;

using GameInputDeviceManager manager = GameInputDeviceManager.Create();
manager.RefreshDevices();

if (!manager.TryGetFirstGamepad(out GameInputDevice? device, out _))
{
    Console.WriteLine("目前沒有可測試 Force Feedback 的裝置。");
    return;
}

GameInputForceFeedbackEnvelope envelope = GameInputForceFeedback.Envelope(sustainDuration: 250_000);
GameInputForceFeedbackMagnitude magnitude = GameInputForceFeedback.Magnitude(normal: 0.25f);
GameInputForceFeedbackParams effectParams = GameInputForceFeedback.SineWave(magnitude, envelope, frequency: 20);

if (!device!.TryCreateForceFeedbackEffect(0, effectParams, out GameInputForceFeedbackEffect? effect))
{
    Console.WriteLine("裝置不支援指定的 Force Feedback effect。");
    return;
}

using (effect)
{
    effect.State = GameInputFeedbackEffectState.GameInputFeedbackEffectRunning;
}
```

## Raw report 複製

Raw report snapshot 會複製資料，不保留原生 report 生命週期。只有在裝置與應用程式都需要低階 HID/Raw report 時才使用這條路徑。

```csharp
using System;
using InputWeave.GameInput;

using GameInputDeviceManager manager = GameInputDeviceManager.Create();

RawDeviceReportSnapshot? report = manager.GetCurrentRawReport();
if (report is null)
{
    Console.WriteLine("目前沒有 raw device report。");
    return;
}

byte[] data = report.GetData();
Console.WriteLine($"Raw report {report.Info.Id}: {data.Length} bytes");
```

## 執行階段缺失排除

`GameInputRuntime.TryProbe` 可在建立 client 前檢查目前載入原則、候選執行階段、HRESULT 與 Win32 錯誤碼。InputWeave 會用受控載入器對齊 Microsoft C++ 載入器的執行階段選擇行為，但包裝套件不會散佈或安裝 `GameInputRedist.msi`、`GameInputRedist.dll` 或原生橋接 DLL；應用程式安裝流程仍需負責安裝 Microsoft 支援的 GameInput 可轉散發套件。目前也不宣告 NativeAOT、trimming 或 single-file 發佈相容性。

```csharp
using System;
using InputWeave.GameInput;

if (GameInputRuntime.TryProbe(out GameInputRuntimeProbeInfo info))
{
    Console.WriteLine($"GameInput 執行階段可用：{info.SelectedModuleKind}");
    Console.WriteLine($"路徑：{info.SelectedModulePath}");
    Console.WriteLine($"版本：{info.SelectedFileVersion?.ToString() ?? "(未知)"}");
    return;
}

Console.WriteLine("找不到可用的 GameInput 執行階段。");
Console.WriteLine($"載入原則：{info.LoaderPolicy}");
Console.WriteLine($"HRESULT：0x{info.HResult:X8}");
Console.WriteLine($"Win32 錯誤：{info.Win32Error}");

foreach (GameInputRuntimeCandidateInfo candidate in info.Candidates)
{
    Console.WriteLine(
        $"{candidate.ModuleKind}: Exists={candidate.Exists}, " +
        $"Path={candidate.ModulePath}, " +
        $"Version={candidate.FileVersion?.ToString() ?? "(未知)"}, " +
        $"Load=0x{candidate.LoadHResult:X8}, " +
        $"EntryPoint=0x{candidate.GetProcAddressHResult:X8}, " +
        $"Win32={candidate.Win32Error}");
}

Console.WriteLine("請確認 Windows 內建 GameInput 執行階段可用，或在應用程式安裝流程中安裝 Microsoft GameInput 可轉散發套件。");
```
