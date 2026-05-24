# 競品完成度比較

本文件用來追蹤 `InputWeave.GameInput` 與公開 C#/.NET GameInput wrapper 的差距。資料以 NuGet 與公開 GitHub repo 為準；不反編譯私有或未公開套件內容。

最後核對日期：2026-05-24

## 比較摘要

| 專案 | 最新公開狀態 | 框架 | 估計完成度 | 對 InputWeave 的意義 |
| --- | --- | --- | --- | --- |
| `GameInput.Net` | NuGet `1.2.2`，GitHub repo 於 2026-05-02 封存 | `net8.0-windows7.0` | 70-75% | 高階 wrapper API 最完整的最低追平線。 |
| `GameInputSharp.Core` | NuGet `0.1.0-alpha` | `net8.0`、`net8.0-windows7.0`、`net9.0` | 65-75% | manager 型入口、samples 與 haptics 易用性值得參考。 |
| `SharpGameInput` | GitHub 原始碼，未見 NuGet package | 多目標原始碼 | 低階 interop 70-80%；高階 wrapper 50-60% | raw vtable binding、callback 與 size checks 是 ABI 可信度基準。 |
| `Starward.GameInput` | NuGet `0.3.1` | `net9.0` | 50-60% | SharpGameInput fork，偏應用情境，不作為通用 wrapper 主基準。 |
| `InputWeave.GameInput` | 本 repo，`Microsoft.GameInput 3.4.218`，版本 `v0.0.1` | `net48;net10.0-windows` | 100% v3.x coverage | 以完整 v3.x API、雙 TFM、VS2026、由 `GameInput.h` 產生的 interop、追版自動化與正體中文文件做差異化。 |

## 已追平項目

- 高階 facade：`GameInputClient` 已提供初始化、timestamp、focus policy、current/next/previous reading、device enumeration、device lookup、aggregate device、dispatcher 與 callback 註冊。
- 裝置包裝：`GameInputDevice` 已提供 device info、haptic info、rumble、force feedback、mapper、extra axis/button、raw report 與 raw output。
- Reading 包裝：`GameInputReading` 已提供 controller、keyboard、mouse、sensors、arcade stick、flight stick、gamepad、racing wheel 與 raw report 讀取。
- 易用性：`GameInputDeviceManager` 提供裝置快取、事件佇列、polling API；`GameInputDeviceInfoSnapshot` 與 `GameInputHapticInfoSnapshot` 提供 managed snapshot。
- 進階功能：`GameInputForceFeedback` 提供 managed builder；`GameInputDispatcher` 提供 safe wait handle；raw report 支援 byte array 區段與 `net10.0-windows` span API。
- 低階驗證：bindings generator 已產生 enum、constants、HRESULT、IID、callback delegate、struct layout、COM interface 與 `gameinput-abi-manifest.json`，`Verify-GameInputBindings.ps1` 會比對目前 `GameInput.h` 的所有產生檔。
- 平台定位：維持 `net48;net10.0-windows`，競品目前未同時覆蓋這組目標框架。

## 尚未完全追平項目

- `docs/gameinput-api-coverage.md` 標示 v0.0.1 缺口為 0。
- 實體硬體 smoke 測試需在有 GameInput runtime 與裝置的機器上，以 `INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS=1` 額外啟用。

## 差異化目標

- 每次更新 `Microsoft.GameInput` 後，都必須更新 `eng/gameinput-baseline.json`、`Interop/Generated` 產生檔、ABI manifest 與 `docs/gameinput-version-report.md`。
- 每次 release 前都必須執行 `eng/Verify-GameInputCoverage.ps1`，確認 coverage report 沒有缺口。
- 若 NuGet 或 GitHub 競品版本有更新，需同步刷新本文件的版本、框架與完成度判斷。
- 不複製競品 API 命名或實作，只以功能覆蓋面作為追平基準。
