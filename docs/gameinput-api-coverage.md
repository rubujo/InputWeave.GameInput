# InputWeave.GameInput v0.0.1 API 覆蓋率

本報告定義 `InputWeave.GameInput v0.0.1` 的 100% 完整度標準。範圍鎖定 `Microsoft.GameInput 3.4.259`、GameInput API 版本 `3`，並以 `native/include/GameInput.h` 的公開介面範圍為準。

最後核對日期：2026-07-21

## 覆蓋率摘要

- 低階互通層：100%
- 高階包裝 API：100%
- 測試與文件：100%
- 缺口：0

## 低階互通層

`src/InputWeave.GameInput/Interop/Generated/` 由 `GameInput.h` 產生並由 `eng/Verify-GameInputBindings.ps1` 驗證：

- 列舉：27 / 27
- 結構：32 / 32
- 回呼委派：4 / 4
- COM Interface：7 / 7
- HRESULT：10 / 10
- 常數、IID、方法順序：已驗證

## 高階包裝 API

主要 GameInput 能力皆有受控 API：

- 裝置：`GameInputDevice`、`GameInputDeviceInfoSnapshot`、`GameInputDeviceManager`，並提供依能力選擇第一個裝置的 helper
- 讀取資料：Controller Axis/Button/Switch、Keyboard、Mouse、Sensors、Arcade Stick、Flight Stick、Gamepad、Racing Wheel、Raw Report，並提供不持有原生生命週期的 snapshot
- 回呼：Reading、Device、System Button、Keyboard Layout，並由 `GameInputCallbackRegistration` 管理 Unregister/Dispose
- 分派器：`GameInputDispatcher` 與 `GameInputDispatcherWaitHandle`
- 映射：Gamepad、Flight Stick、Racing Wheel、Arcade Stick Mapping
- 原始報告：`byte[]` 區段 API、`net10.0-windows` Span API 與 `RawDeviceReportSnapshot`
- Force Feedback / Haptics：`GameInputForceFeedback` Builder、具名 effect helper、`GameInputForceFeedbackEffect`、`GameInputHapticInfoSnapshot`
- Rumble：依能力遮罩的 opt-in helper 與 `GameInputRumbleScope`
- 聚合裝置：Create / Disable
- 執行階段載入器：`GameInputRuntime` 提供與 Microsoft C++ 載入器對齊的受控執行階段選擇與探測診斷
- 防禦性邊界：原生回報的數量與大小（raw report 上限 `GameInputRawDeviceReport.MaxRawDataSize`、元素數量與字串長度內部上限）超過上限時視為裝置或驅動程式回報異常，`Try*` 方法回傳 false、直接呼叫的方法拋出 `InvalidOperationException`；`FindDeviceFromPlatformString` 平台字串長度上限為 `GameInputClient.MaxPlatformStringLength`
- 發佈支援界線：`net10.0-windows` 已實際跑過 `dotnet publish -p:PublishAot=true` 端對端驗證（獨立探測專案 + 實體硬體，詳見 README「支援範圍」段落）；不宣告 single-file 發佈相容性，不包含原生橋接 DLL
- 非同步 API：`GameInputClient.EnumerateDevicesAsync`/`WaitForReadingAsync`/`WaitForGamepadAsync`、
  `GameInputDeviceManager.RefreshDevicesAsync`/`WaitForDeviceEventAsync`，以 `Task`/`TaskCompletionSource` 包裝，
  一次性原生回呼會延後到背景執行緒解除註冊，避免在原生回呼執行緒內同步 `Dispose`
- 事件與 `IObservable<T>`：`GameInputDeviceManager.DeviceChanged`（標準 C# event）與 `DeviceChanges`
  （不依賴 `System.Reactive` 的 `IObservable<T>`），手動輪詢與事件訂閱可混用，`Dispose()` 時觸發 `OnCompleted()`
- 依賴注入：`AddGameInputClient()`/`AddGameInputDeviceManager()`（`Microsoft.Extensions.DependencyInjection.Abstractions`）
  以 Singleton 註冊，容器釋放時一併釋放 GameInput 資源
- Snapshot 值相等性：所有讀取/裝置資訊 snapshot 皆為 `readonly record struct`；含集合欄位（陣列、標籤清單等）的型別
  提供逐一比較內容的 `IEquatable<T>` 覆寫，原生指標欄位（僅供診斷）不參與比較

## 測試與驗收

必跑驗收命令：

```powershell
dotnet restore InputWeave.GameInput.slnx
dotnet build InputWeave.GameInput.slnx -c Release
dotnet test InputWeave.GameInput.slnx -c Release
dotnet format InputWeave.GameInput.slnx --verify-no-changes
pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated
pwsh ./eng/Verify-GameInputBindings.ps1
pwsh ./eng/Verify-GameInputCoverage.ps1
pwsh ./eng/Validate-TextEncoding.ps1
pwsh ./eng/Validate-AgentDocs.ps1
dotnet pack src/InputWeave.GameInput/InputWeave.GameInput.csproj -c Release
```

硬體抽測以 `INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS=1` 啟用。沒有 GameInput 執行階段或實體裝置時，測試必須清楚略過或標示未定。

## 已知例外

無。`v0.0.1` 的 100% 完整度以目前基準的 GameInput v3.x 公開標頭介面範圍與本報告列出的高階包裝能力為準。
