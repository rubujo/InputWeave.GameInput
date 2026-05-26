# InputWeave.GameInput 常見情境指南

本文件提供可以直接放進主控台應用程式或方法中的常見整合片段。正式應用程式仍應依自己的遊戲迴圈、執行緒模型與安裝流程調整錯誤處理。

## 遊戲控制器輪詢迴圈

這個片段會列舉裝置、以能力選出第一個支援 Gamepad 的裝置，然後用簡單迴圈讀取目前快照。沒有遊戲控制器或暫時沒有讀取資料時，程式會清楚略過。

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using InputWeave.GameInput;
using InputWeave.GameInput.Interop;

using GameInputDeviceManager manager = GameInputDeviceManager.Create();
IReadOnlyList<GameInputDeviceInfoSnapshot> snapshots = manager.RefreshDevices();

int gamepadIndex = FindFirstGamepad(snapshots);
if (gamepadIndex < 0)
{
    Console.WriteLine("目前沒有支援 Gamepad 的 GameInput 裝置。");
    return;
}

GameInputDevice gamepadDevice = manager.Devices[gamepadIndex];
Console.WriteLine($"使用裝置：{GetDisplayName(snapshots[gamepadIndex])}");

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

static int FindFirstGamepad(IReadOnlyList<GameInputDeviceInfoSnapshot> snapshots)
{
    for (int index = 0; index < snapshots.Count; index++)
    {
        if ((snapshots[index].SupportedInput & GameInputKind.GameInputKindGamepad) == GameInputKind.GameInputKindGamepad)
        {
            return index;
        }
    }

    return -1;
}

static string GetDisplayName(GameInputDeviceInfoSnapshot snapshot)
{
    return string.IsNullOrWhiteSpace(snapshot.DisplayName) ? "(未命名裝置)" : snapshot.DisplayName;
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

## 明確啟用震動

震動功能應該由使用者明確啟用。這個片段預設不震動，只有傳入 `--rumble` 時才對支援裝置輸出短暫低強度震動，並用 `finally` 清除狀態。

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using InputWeave.GameInput;
using InputWeave.GameInput.Interop;

bool enableRumble = args.Any(static argument => string.Equals(argument, "--rumble", StringComparison.OrdinalIgnoreCase));
if (!enableRumble)
{
    Console.WriteLine("未傳入 --rumble；預設不觸發任何硬體震動。");
    return;
}

using GameInputDeviceManager manager = GameInputDeviceManager.Create();
IReadOnlyList<GameInputDeviceInfoSnapshot> snapshots = manager.RefreshDevices();

int deviceIndex = FindFirstRumbleDevice(snapshots);
if (deviceIndex < 0)
{
    Console.WriteLine("目前沒有宣告支援震動馬達的 GameInput 裝置。");
    return;
}

GameInputDevice device = manager.Devices[deviceIndex];
GameInputRumbleMotors motors = snapshots[deviceIndex].SupportedRumbleMotors;
GameInputRumbleParams rumble = CreateLowIntensityRumble(motors);

try
{
    device.SetRumbleState(rumble);
    Thread.Sleep(250);
    Console.WriteLine($"已短暫觸發低強度 Rumble：{motors}");
}
finally
{
    device.ClearRumbleState();
}

static int FindFirstRumbleDevice(IReadOnlyList<GameInputDeviceInfoSnapshot> snapshots)
{
    for (int index = 0; index < snapshots.Count; index++)
    {
        if (snapshots[index].SupportedRumbleMotors != GameInputRumbleMotors.GameInputRumbleNone)
        {
            return index;
        }
    }

    return -1;
}

static GameInputRumbleParams CreateLowIntensityRumble(GameInputRumbleMotors motors)
{
    float lowFrequency = SupportsRumbleMotor(motors, GameInputRumbleMotors.GameInputRumbleLowFrequency) ? 0.15f : 0;
    float highFrequency = SupportsRumbleMotor(motors, GameInputRumbleMotors.GameInputRumbleHighFrequency) ? 0.15f : 0;
    float leftTrigger = SupportsRumbleMotor(motors, GameInputRumbleMotors.GameInputRumbleLeftTrigger) ? 0.10f : 0;
    float rightTrigger = SupportsRumbleMotor(motors, GameInputRumbleMotors.GameInputRumbleRightTrigger) ? 0.10f : 0;
    return GameInputForceFeedback.Rumble(lowFrequency, highFrequency, leftTrigger, rightTrigger);
}

static bool SupportsRumbleMotor(GameInputRumbleMotors supportedMotors, GameInputRumbleMotors motor)
{
    return (supportedMotors & motor) == motor;
}
```

## 執行階段缺失排除

`GameInputRuntime.TryProbe` 可在建立 client 前檢查目前載入原則、候選執行階段、HRESULT 與 Win32 錯誤碼。InputWeave 會用受控載入器對齊 Microsoft C++ 載入器的執行階段選擇行為，但包裝套件不會散佈或安裝 `GameInputRedist.msi`、`GameInputRedist.dll` 或原生橋接 DLL；應用程式安裝流程仍需負責安裝 Microsoft 支援的 GameInput 可轉散發套件。

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
