# InputWeave.GameInput v0.0.1 API Coverage

本報告定義 `InputWeave.GameInput v0.0.1` 的 100% 完整度標準。範圍鎖定 `Microsoft.GameInput 3.4.218`、GameInput API version `3`，並以 `native/include/GameInput.h` 的公開 surface 為準。

最後核對日期：2026-05-24

## Coverage Summary

- 低階 interop：100%
- 高階 wrapper：100%
- 測試與文件：100%
- 缺口：0

## 低階 Interop

`src/InputWeave.GameInput/Interop/Generated/` 由 `GameInput.h` 產生並由 `eng/Verify-GameInputBindings.ps1` 驗證：

- enum：27 / 27
- struct：32 / 32
- callback delegate：4 / 4
- COM interface：7 / 7
- HRESULT：10 / 10
- constants、IID、method order：已驗證

## 高階 Wrapper

主要 GameInput 能力皆有 managed API：

- device：`GameInputDevice`、`GameInputDeviceInfoSnapshot`、`GameInputDeviceManager`
- reading：controller axis/button/switch、keyboard、mouse、sensors、arcade stick、flight stick、gamepad、racing wheel、raw report
- callback：reading、device、system button、keyboard layout，並由 `GameInputCallbackRegistration` 管理 unregister/dispose
- dispatcher：`GameInputDispatcher` 與 `GameInputDispatcherWaitHandle`
- mapper：gamepad、flight stick、racing wheel、arcade stick mapping
- raw report：`byte[]` 區段 API 與 `net10.0-windows` span API
- force feedback / haptics：`GameInputForceFeedback` builder、`GameInputForceFeedbackEffect`、`GameInputHapticInfoSnapshot`
- aggregate device：create / disable
- runtime loader：`GameInputRuntime` 提供 Microsoft C++ loader parity 的 managed runtime selection 與 probe 診斷

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

硬體 smoke 測試以 `INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS=1` 啟用。沒有 GameInput runtime 或實體裝置時，測試必須清楚略過或標示 inconclusive。

## Documented Exceptions

無。`v0.0.1` 的 100% 完整度以目前 baseline 的 GameInput v3.x 公開 header surface 與本報告列出的高階 wrapper 能力為準。
