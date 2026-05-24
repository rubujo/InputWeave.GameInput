# InputWeave.GameInput

`InputWeave.GameInput v0.0.1` 是 Microsoft GameInput 的 C# 分層包裝程式庫，目標是同時支援 .NET Framework 與最新版 .NET，並讓 GameInput 版本追蹤、繫結產生與發佈檢查可以自動化。

定位上，本專案不是只做 gamepad polling；目標是覆蓋 GameInput v3.x 的低階 interop 與高階 C# wrapper，並以 `net48;net10.0-windows`、VS2026、最新 `Microsoft.GameInput` baseline、由 `GameInput.h` 產生的 interop 原始碼與 ABI manifest 追版流程，補足現有公開競品的維護與版本追蹤缺口。

目前專案基準：

- Visual Studio 2026 / `.slnx`
- `net48;net10.0-windows`
- NuGet / MSBuild 版本：`0.0.1`
- Git tag / Release 名稱：`v0.0.1`
- `Microsoft.GameInput` `3.4.218`
- GameInput API version `3`
- API coverage：`docs/gameinput-api-coverage.md` 標示缺口為 0
- 文件、腳本輸出與人工註解使用正體中文台灣用語

## 使用方式

```csharp
using System;
using System.Collections.Generic;
using InputWeave.GameInput;
using InputWeave.GameInput.Interop;

using GameInputClient client = GameInputClient.Create();
client.SetFocusPolicy(GameInputFocusPolicy.GameInputEnableBackgroundInput);

GamepadReadingSnapshot? snapshot = client.GetCurrentGamepad();
if (snapshot is not null)
{
    GameInputGamepadButtons buttons = snapshot.State.Buttons;
}

IReadOnlyList<GameInputDevice> devices = client.EnumerateDevices(GameInputKind.GameInputKindGamepad);
using GameInputReading? reading = client.GetCurrentReading(GameInputKind.GameInputKindKeyboard);
GameInputKeyState[] keys = reading?.GetKeyState() ?? Array.Empty<GameInputKeyState>();
```

Managed API 也提供裝置 manager、snapshot、safe wait handle、force feedback builder 與 raw report 區段 API：

```csharp
using GameInputDeviceManager manager = GameInputDeviceManager.Create();
IReadOnlyList<GameInputDeviceInfoSnapshot> devices = manager.RefreshDevices();

GameInputForceFeedbackEnvelope envelope = GameInputForceFeedback.Envelope();
GameInputForceFeedbackMagnitude magnitude = GameInputForceFeedback.Magnitude(normal: 0.5f);
GameInputForceFeedbackParams parameters = GameInputForceFeedback.Constant(magnitude, envelope);
```

範例專案在 `samples/InputWeave.GameInput.Samples`，涵蓋 polling、callbacks、device manager、dispatcher safe handle、haptics 與 rumble 路徑。

## 建置與驗證

```powershell
dotnet restore
dotnet build InputWeave.GameInput.slnx -c Release
dotnet test InputWeave.GameInput.slnx -c Release
dotnet format InputWeave.GameInput.slnx --verify-no-changes
pwsh ./eng/Validate-TextEncoding.ps1
pwsh ./eng/Validate-AgentDocs.ps1
pwsh ./eng/Verify-GameInputBindings.ps1
pwsh ./eng/Verify-GameInputCoverage.ps1
```

`Verify-GameInputBindings.ps1` 會重新從目前 baseline 的 `GameInput.h` 產生 enum、constants、HRESULT、IID、callback delegate、struct layout、COM interface 與 ABI manifest，確認 repo 內 `src/InputWeave.GameInput/Interop/Generated/` 的產生檔沒有與目前 header 脫鉤。

`Verify-GameInputCoverage.ps1` 會驗證 generated interop、高階 wrapper surface 與 [docs/gameinput-api-coverage.md](docs/gameinput-api-coverage.md) 的 v0.0.1 coverage 報告一致。

## 競品追蹤

本專案以 `GameInput.Net` 的高階 wrapper API 面、`SharpGameInput` 的低階 ABI 可信度，以及 `GameInputSharp.Core` 的易用性作為公開競品基準，但不複製其 API 命名或程式碼。

目前差距與追趕狀態請看 [docs/competitive-comparison.md](docs/competitive-comparison.md)。

## GameInput Redist

`Microsoft.GameInput` NuGet 套件包含 `GameInputRedist.msi`，但不會自動安裝。PC 應用程式發佈時必須把該 redist 納入安裝流程；本 wrapper 只記錄與驗證 redist 雜湊，不會把 MSI 包進 wrapper NuGet。

更多細節請看 [docs/gameinput-redist.md](docs/gameinput-redist.md)。
