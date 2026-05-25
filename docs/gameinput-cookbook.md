# InputWeave.GameInput 常見情境 Cookbook

本文件提供可以直接放進 console app 或方法中的常見整合片段。正式應用程式仍應依自己的遊戲迴圈、執行緒模型與安裝流程調整錯誤處理。

## Gamepad polling loop

這個片段會列舉裝置、以能力選出第一個支援 gamepad 的裝置，然後用簡單迴圈讀取目前 snapshot。沒有 gamepad 或暫時沒有 reading 時，程式會清楚略過。

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
    Console.WriteLine("目前沒有支援 gamepad 的 GameInput 裝置。");
    return;
}

GameInputDevice gamepadDevice = manager.Devices[gamepadIndex];
Console.WriteLine($"使用裝置：{GetDisplayName(snapshots[gamepadIndex])}");

for (int frame = 0; frame < 600; frame++)
{
    GamepadReadingSnapshot? gamepad = manager.GetCurrentGamepad(gamepadDevice);
    if (gamepad is null)
    {
        Console.WriteLine("目前沒有可讀取的 gamepad snapshot。");
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

## Device hotplug 與 callback

這個片段示範建立 dispatcher、safe wait handle 與 device callback。沒有插拔或狀態變化時，不收到 callback 是正常結果；callback 內的 `GameInputDevice` 只在 callback 執行期間有效，不要存起來跨 callback 使用。

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
    ? "Dispatcher safe wait handle 無效；改用直接 Dispatch。"
    : "Dispatcher safe wait handle 已建立。");

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

## Rumble opt-in

Rumble 應該由使用者明確啟用。這個片段預設不震動，只有傳入 `--rumble` 時才對支援裝置輸出短暫低強度震動，並用 `finally` 清除狀態。

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
    Console.WriteLine("目前沒有宣告支援 rumble motor 的 GameInput 裝置。");
    return;
}

GameInputDevice device = manager.Devices[deviceIndex];
GameInputRumbleMotors motors = snapshots[deviceIndex].SupportedRumbleMotors;
GameInputRumbleParams rumble = CreateLowIntensityRumble(motors);

try
{
    device.SetRumbleState(rumble);
    Thread.Sleep(250);
    Console.WriteLine($"已短暫觸發低強度 rumble：{motors}");
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

## Runtime missing troubleshooting

`GameInputRuntime.TryProbe` 可在建立 client 前檢查目前 loader policy、候選 runtime、HRESULT 與 Win32 錯誤碼。InputWeave 會用 managed loader 對齊 Microsoft C++ loader 的 runtime selection 行為，但 wrapper NuGet 不會散佈或安裝 `GameInputRedist.msi`、`GameInputRedist.dll` 或 native shim；應用程式安裝流程仍需負責安裝 Microsoft 支援的 GameInput redist。

```csharp
using System;
using InputWeave.GameInput;

if (GameInputRuntime.TryProbe(out GameInputRuntimeProbeInfo info))
{
    Console.WriteLine($"GameInput runtime 可用：{info.SelectedModuleKind}");
    Console.WriteLine($"Path：{info.SelectedModulePath}");
    Console.WriteLine($"Version：{info.SelectedFileVersion?.ToString() ?? "(未知)"}");
    return;
}

Console.WriteLine("找不到可用的 GameInput runtime。");
Console.WriteLine($"Loader policy：{info.LoaderPolicy}");
Console.WriteLine($"HRESULT：0x{info.HResult:X8}");
Console.WriteLine($"Win32 error：{info.Win32Error}");

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

Console.WriteLine("請確認 Windows 內建 GameInput runtime 可用，或在應用程式安裝流程中安裝 Microsoft GameInput redist。");
```
