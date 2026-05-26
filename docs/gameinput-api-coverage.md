# InputWeave.GameInput v0.0.1 API 覆蓋率

本報告定義 `InputWeave.GameInput v0.0.1` 的 100% 完整度標準。範圍鎖定 `Microsoft.GameInput 3.4.218`、GameInput API 版本 `3`，並以 `native/include/GameInput.h` 的公開介面範圍為準。

最後核對日期：2026-05-27

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
- 發佈支援界線：目前不宣告 NativeAOT、trimming 或 single-file 發佈相容性，不包含原生橋接 DLL

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
