# InputWeave.GameInput

`InputWeave.GameInput` 是 Microsoft GameInput 的 C# 分層包裝程式庫，支援 .NET Framework `net48` 與 `.NET 10` Windows 應用程式。專案提供由 `GameInput.h` 產生的低階互通層、高階 C# API、執行階段載入診斷與發佈前驗證流程。

## AI 生成與維護聲明

本專案的程式碼、文件與維護流程主要由 AI 代理產生、整理與更新；人工使用者負責審閱、決策、驗證與發佈。使用本專案時，請依實際環境再次驗證。

## 授權

本儲存庫自有程式碼與文件以 [CC0 1.0 Universal](LICENSE) 發布。`Microsoft.GameInput`、`GameInputRedist.msi`、GameInput API、Microsoft 商標與其他第三方資產不屬於本專案 CC0 授權範圍。

## 套件資訊

- 套件版本：`0.0.1`
- 發佈標籤：`v0.0.1`
- 目標框架：`net48;net10.0-windows`
- GameInput 基準：`Microsoft.GameInput` `3.4.218`，API 版本 `3`
- 授權中繼資料：`CC0-1.0`
- API 覆蓋率：[docs/gameinput-api-coverage.md](docs/gameinput-api-coverage.md)

## 支援範圍

本套件支援一般 .NET Framework 與 .NET Windows 應用程式。`net48` 維持傳統 `[ComImport]` COM interop 路徑；`net10.0-windows` 改用不依賴 CLR 內建 COM 封送的裸 vtable 投影（`delegate* unmanaged` 函式指標 + 手動 `AddRef`/`Release`），巢狀 COM 物件也能確定性釋放。

`net10.0-windows` 路徑已實際跑過 `dotnet publish -p:PublishAot=true` 端對端驗證：用一個獨立探測專案引用本函式庫，實測裝置列舉、非同步 API、Snapshot 相等性／雜湊、事件、依賴注入解析等主要路徑，`ilc` 原生程式碼產生與連結皆順利完成，產生的原生執行檔在真實 GameInput 執行階段（含實體 Xbox 控制器）下行為正常，過程中發現並修正了 3 處 trim/AOT 分析錯誤（泛型 `Marshal.PtrToStructure<T>`／`Marshal.SizeOf(Type)` 呼叫缺少必要標注，詳見 `GameInputDeviceInfoSnapshot.cs`／`GameInputMapper.cs`）。本套件不包含原生橋接 DLL；發佈前仍建議在目標環境自行跑一輪驗證，尤其是還沒被涵蓋到的低階 Interop 逃生口路徑。

## 基本使用

```csharp
using InputWeave.GameInput;
using InputWeave.GameInput.Interop;

using GameInputDeviceManager manager = GameInputDeviceManager.Create();
manager.RefreshDevices();

if (manager.TryGetFirstGamepad(out GameInputDevice? gamepadDevice, out GameInputDeviceInfoSnapshot? gamepadInfo))
{
    GamepadReadingSnapshot? snapshot = manager.GetCurrentGamepad(gamepadDevice);
    if (snapshot is not null)
    {
        GameInputGamepadButtons buttons = snapshot.State.Buttons;
    }
}

_ = gamepadInfo?.DisplayName;
```

裝置管理、各輸入種類快照、分派器、Safe Wait Handle、Rumble scope、Force Feedback、原始報告、非同步 API、事件／`IObservable<T>` 與依賴注入註冊，請參考 [GameInput 常見情境指南](docs/gameinput-cookbook.md)。

## 何時該使用低階 `InputWeave.GameInput.Interop`

高階 API（`GameInputDeviceManager`、`GameInputClient`、`GameInputDevice` 等）涵蓋絕大多數情境，一般使用不需要接觸 `InputWeave.GameInput.Interop` 命名空間。以下情境才需要往下降到低階層：

- 需要呼叫尚未包裝成高階便利方法的原生 GameInput API（可對照 [docs/gameinput-api-coverage.md](docs/gameinput-api-coverage.md) 確認涵蓋範圍）。
- 需要自訂原生結構的封送方式，或要整合既有的原生 C++/COM 呼叫端。
- 需要診斷層級的原始資料（例如 `GameInputDeviceInfo` 的原生指標欄位），而不是高階快照已複製的欄位。

`InputWeave.GameInput.Interop` 內的列舉、常數與結構是公開型別，可直接使用；COM 介面本身則是 `internal`，一般不會也不需要直接操作。多數高階包裝類別會透過 `NativeInterface` 內部屬性存取對應的低階介面，但這個逃生口主要是給函式庫內部使用；一般應用程式若發現高階 API 涵蓋不到的情境，建議優先回報需求，而不是依賴內部實作細節。

## 範例

範例專案位於 `samples/InputWeave.GameInput.Samples`。預設執行只會初始化、列舉裝置、讀取遊戲控制器狀態並示範回呼模式，不會觸發硬體震動。

```powershell
dotnet run --project samples/InputWeave.GameInput.Samples
```

若要測試支援裝置的震動功能，必須明確傳入 `--rumble`。範例只會短暫輸出低強度震動，並在結束前清除震動狀態。

```powershell
dotnet run --project samples/InputWeave.GameInput.Samples -- --rumble
```

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

`Verify-GameInputBindings.ps1` 會重新從目前基準的 `GameInput.h` 產生低階互通層與 ABI 資訊清單，確認儲存庫內的產生檔沒有與官方標頭脫鉤。`Verify-GameInputCoverage.ps1` 會確認高階 API 與覆蓋率文件一致。

## GameInput 可轉散發套件

InputWeave 使用受控載入器對齊 Microsoft C++ 載入器的執行階段選擇行為，依序探測 Windows System32 內的 `GameInput.dll`、System32 內的 `GameInputRedist.dll`，以及 `HKLM\SOFTWARE\Microsoft\GameInput\RedistDir` 指向的 `GameInputRedist.dll`。當可轉散發執行階段版本大於或等於 Windows 內建執行階段時，會優先載入可轉散發執行階段。

載入流程會避免從應用程式目錄、目前工作目錄或 `PATH` 載入同名 DLL。需要診斷時，可呼叫 `GameInputRuntime.TryProbe(out GameInputRuntimeProbeInfo info)` 檢查候選路徑、選擇結果、HRESULT 與 Win32 錯誤碼。

本包裝套件不會散佈或安裝 `GameInputRedist.msi`、`GameInputRedist.dll` 或原生橋接 DLL。發佈 Windows PC 應用程式時，應用程式安裝流程仍需負責安裝 Microsoft 支援的 GameInput 可轉散發套件。詳細資訊請參考 [docs/gameinput-redist.md](docs/gameinput-redist.md)。
