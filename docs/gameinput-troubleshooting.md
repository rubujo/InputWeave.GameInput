# InputWeave.GameInput 常見錯誤與排查

本文件整理應用程式整合時最常見的問題。若需要完整情境範例，請搭配 [GameInput 常見情境指南](gameinput-cookbook.md) 閱讀。

## 找不到 GameInput runtime

常見症狀：

- `GameInputDeviceManager.Create()` 或 `GameInputClient.Create()` 拋出 `DllNotFoundException`。
- `GameInputRuntime.TryProbe(out GameInputRuntimeProbeInfo info)` 回傳 `false`。

排查方式：

1. 先呼叫 `GameInputRuntime.TryProbe(out GameInputRuntimeProbeInfo info)`，列印 `info.HResult`、`info.Win32Error` 與 `info.Candidates`。
2. 確認目標機器有 Windows 內建 `GameInput.dll`，或已由應用程式安裝流程安裝 Microsoft 支援的 `GameInputRedist.msi`。
3. 不要把 `GameInputRedist.dll` 複製到應用程式目錄或依賴 `PATH` 載入；InputWeave 的 loader 只接受 System32 與登錄檔 redist 目錄候選，以降低 DLL 劫持風險。

本套件不會散佈或自動安裝 `GameInputRedist.msi`、`GameInputRedist.dll` 或原生橋接 DLL。發佈端安裝責任請參考 [GameInput 可轉散發套件發佈注意事項](gameinput-redist.md)。

## 找不到裝置或沒有 reading

常見症狀：

- `RefreshDevices()` 回傳空清單。
- `TryGetFirstGamepad(out _, out _)` 回傳 `false`。
- `GetCurrentGamepad()` 或其他 current snapshot API 回傳 `null`。

排查方式：

1. 確認裝置已連線，並且 Windows 可以在系統設定或遊戲控制器工具中看到它。
2. 呼叫 `RefreshDevices(GameInputKind, GameInputDeviceStatus)` 時確認 `inputKind` 與 `statusFilter` 沒有篩掉目標裝置。
3. `GetCurrent*` API 回傳 `null` 不一定是錯誤，可能只是目前沒有該輸入種類的 reading；輪詢迴圈應把 `null` 視為正常暫態。
4. 若剛建立 aggregate device，不要立刻用剛取得的 device ID 查詢；請先等 device callback 或 `GameInputDeviceManager.DeviceChanged` 收到狀態通知。

## Callback 例外沒有直接拋出

常見症狀：

- callback handler 裡的例外沒有傳回呼叫端。
- callback 中同步呼叫 `registration.Dispose()` 拋出 `InvalidOperationException`。

排查方式：

1. 在應用程式啟動時訂閱 `GameInputClient.UnhandledCallbackException`，集中記錄 callback 例外。
2. callback handler 內收到的 `GameInputReading` 或 `GameInputDevice` 只應在 handler 執行期間使用；需要跨執行緒或稍後處理時，請立即轉成 snapshot。
3. 不要在原生 callback 執行緒中同步釋放同一個 `GameInputCallbackRegistration`。需要一次性事件時，請在 callback 內設定旗標，之後由其他執行緒或下一個 frame 釋放。

## 硬體煙霧測試沒有執行

常見症狀：

- `HardwareSmokeCoversManagerDispatcherAndReadingPaths` 被標示為 inconclusive 或 skipped。

排查方式：

1. 在執行測試前設定環境變數：

```powershell
$env:INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS = '1'
dotnet test InputWeave.GameInput.slnx -c Release --filter Hardware
```

2. 確認測試機器有可用的 GameInput runtime。
3. 若這台機器沒有硬體或 runtime，不要把測試結果人工視為通過；請在發佈記錄中標示未執行。

## NativeAOT、trimming 與 single-file

目前 `net10.0-windows` 路徑已實際跑過 `dotnet publish -p:PublishAot=true` 端對端驗證，涵蓋裝置列舉、非同步 API、事件、依賴注入與主要 snapshot 路徑。

仍需注意：

- `.csproj` 目前不宣告 `IsAotCompatible` 或 `IsTrimmable`。
- 本專案不宣告 single-file 發佈相容性。
- 低階 `InputWeave.GameInput.Interop` 逃生口若被應用程式直接使用，仍應在目標發佈形狀下自行驗證。

## 原生回報大小異常

常見症狀：

- 直接呼叫 raw report 或 device info 相關 API 時拋出 `InvalidOperationException`。
- `TryGetRawReportSnapshot` 或高階 `GetCurrentRawReport` 回傳 `false` / `null`。

排查方式：

1. 檢查例外訊息中的數量或位元組大小是否超過 InputWeave 的防禦上限。
2. 若只有特定裝置會發生，優先視為裝置、驅動程式或 runtime 回報異常。
3. 使用 `Try*` API 與 snapshot API 可讓應用程式把異常裝置降級處理，而不是讓輪詢迴圈中斷。
