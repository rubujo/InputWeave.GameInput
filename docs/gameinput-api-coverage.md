# InputWeave.GameInput v0.0.1 API 覆蓋率

本報告定義 `InputWeave.GameInput v0.0.1` 的 100% 完整度標準。範圍鎖定 `Microsoft.GameInput 4.0.300`、GameInput API 版本 `4`，並以 `native/include/GameInput.h` 的公開介面範圍為準。

本報告涵蓋 API 4 主標頭全部公開宣告；逐方法包裝清單為 `eng/gameinput-wrapper-coverage.json`，驗證器會逐項核對原生呼叫。硬體驗證結果另列於 `docs/gameinput-api4-validation.md`，不將宣告覆蓋率當成所有裝置功能的硬體實測。

最後核對日期：2026-10-18

## 覆蓋率摘要

- 低階互通層：100%
- 高階包裝 API：100%
- XML 文件與逐方法包裝清單：100%
- 缺口：0

## 低階互通層

`src/InputWeave.GameInput/Interop/Generated/` 由 `GameInput.h` 產生並由 `eng/Verify-GameInputBindings.ps1` 驗證：

- 列舉：27 / 27
- 結構：33 / 33
- 回呼委派：2 / 2
- COM Interface：7 / 7
- HRESULT：10 / 10
- COM 方法：116 / 116
- 原生函式：2 / 2
- 常數：21 / 21
- 常數、IID、方法順序：已驗證

## 高階包裝 API

主要 GameInput 能力皆有受控 API：

- 裝置：`GameInputDevice`、`GameInputDeviceInfoSnapshot`、`GameInputDeviceManager`，並提供依能力選擇第一個裝置的 helper
- 讀取資料：Controller Axis/Button/Switch、Keyboard、Mouse、Sensors、System Button、Touch、Arcade Stick、Flight Stick、Gamepad、Racing Wheel、Raw Report，並提供不持有原生生命週期的 snapshot；`GameInputClient`／`GameInputDeviceManager` 同時提供回傳可為 null 的 `GetCurrent*` 與直接輸出快照的 `TryGetCurrent*`，snapshot 另有 `IsButtonDown`／`WasButtonPressed`／`WasButtonReleased`（鍵盤為 `IsKeyDown`／`WasKeyPressed`／`WasKeyReleased`）便利方法
- 回呼：Reading 與統一 Device Event（連線／斷線、鍵盤配置、Haptic Info、XInput 索引異動），並由 `GameInputCallbackRegistration` 管理 Unregister/Dispose
- 分派器：`GameInputDispatcher` 與 `GameInputDispatcherWaitHandle`
- 映射：Gamepad、Flight Stick、Racing Wheel、Arcade Stick Mapping
- 燈光：群組能力、RGB 色彩、套用與還原系統預設
- 觸控：表面能力、接觸點數量與獨立接觸點陣列
- 原始報告：功能報告的 Get / Set、`byte[]` 區段 API、`net8.0`／`net10.0` Span API 與 `RawDeviceReportSnapshot`
- Force Feedback / Haptics：`GameInputForceFeedback` Builder、具名 effect helper、`GameInputForceFeedbackEffect`、`GameInputHapticInfoSnapshot`
- Rumble：依能力遮罩的 opt-in helper 與 `GameInputRumbleScope`
- 聚合裝置：Create / Disable
- 執行階段載入器：`GameInputRuntime` 提供與 Microsoft C++ 載入器對齊的受控執行階段選擇與探測診斷
- 防禦性邊界：原生回報的數量與大小（raw report 上限 `GameInputRawDeviceReport.MaxRawDataSize`、元素數量與字串長度內部上限）超過上限時視為裝置或驅動程式回報異常，`Try*` 方法回傳 false、直接呼叫的方法拋出 `InvalidOperationException`；`FindDeviceFromPlatformString` 平台字串長度上限為 `GameInputClient.MaxPlatformStringLength`
- 發佈支援界線：`net8.0` 與 `net10.0` 舊版曾跑過 `dotnet publish -p:PublishAot=true` 端對端驗證；API 4 證據以驗證報告為準（獨立探測專案 + 實體硬體，詳見 README「支援範圍」段落）；不宣告 single-file 發佈相容性，不包含原生橋接 DLL
- 非同步 API：`GameInputClient.EnumerateDevicesAsync`/`WaitForReadingAsync`/`WaitForGamepadAsync`、
  `GameInputDeviceManager.RefreshDevicesAsync`/`WaitForDeviceEventAsync`，以 `Task`/`TaskCompletionSource` 包裝，
  一次性原生回呼會延後到背景執行緒解除註冊，避免在原生回呼執行緒內同步 `Dispose`
- 事件與 `IObservable<T>`：`GameInputDeviceManager.DeviceChanged`（標準 C# event）與 `DeviceChanges`
  （不依賴 `System.Reactive` 的 `IObservable<T>`），手動輪詢與事件訂閱可混用，`Dispose()` 時觸發 `OnCompleted()`
- 依賴注入：`AddGameInputClient()`/`AddGameInputDeviceManager()`（`Microsoft.Extensions.DependencyInjection.Abstractions`）
  以 Singleton 註冊，容器釋放時一併釋放 GameInput 資源
- Snapshot 值相等性：所有讀取/裝置資訊 snapshot 皆為 `readonly record struct`；含集合欄位（陣列、標籤清單等）的型別
  提供逐一比較內容的 `IEquatable<T>` 覆寫，裝置資訊已完全移除原生指標欄位

## 測試與驗收

必跑驗收命令：

```powershell
dotnet restore InputWeave.GameInput.slnx
dotnet build InputWeave.GameInput.slnx -c Release
dotnet test InputWeave.GameInput.slnx -c Release
dotnet format InputWeave.GameInput.slnx --verify-no-changes
pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated
pwsh ./eng/Show-GameInputGitHubReleases.ps1 -FailOnUnreviewed
pwsh ./eng/Verify-GameInputBindings.ps1
pwsh ./eng/Verify-GameInputCoverage.ps1
pwsh ./eng/Validate-TextEncoding.ps1
pwsh ./eng/Validate-AgentDocs.ps1
dotnet pack src/InputWeave.GameInput/InputWeave.GameInput.csproj -c Release
```

硬體抽測以 `INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS=1` 啟用。沒有 GameInput 執行階段或實體裝置時，測試必須清楚略過或標示未定。

## 已知例外

無。`v0.0.1` 的 100% 完整度以目前基準的 GameInput API 4 公開標頭介面範圍與本報告列出的高階包裝能力為準。
