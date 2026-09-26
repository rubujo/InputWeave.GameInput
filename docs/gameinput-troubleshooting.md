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

## 裝置列舉很慢

`EnumerateDevices`、`RefreshDevices` 使用 GameInput 的阻塞式列舉（`GameInputBlockingEnumeration`），時間幾乎都花在原生 `RegisterDeviceCallback` 等待初始回呼完成。實測 GameInput 3.5.274 與 3.5.278 每次都約 960 毫秒，與輸入種類、裝置數量無關。

處理方式：

1. 不要在 UI 執行緒或每一幀的遊戲迴圈中呼叫；UI 程式請改用 `EnumerateDevicesAsync`／`RefreshDevicesAsync`。
2. 需要持續追蹤裝置時，建立一次 `GameInputDeviceManager` 並訂閱 `DeviceChanged`（內部使用非同步列舉，不會卡住呼叫端），只在收到連線／斷線事件時才重新整理，而不是定期重新列舉。

## `SupportedInput` 顯示成數字或比對不成立

常見症狀：

- 印出 `GameInputDeviceInfoSnapshot.SupportedInput` 時得到數字（例如 Xbox 控制器顯示 `17039367`），而不是 `GameInputKindGamepad, ...` 這類名稱。
- `info.SupportedInput == GameInputKind.GameInputKindGamepad` 對遊戲控制器不成立。

原因：GameInput 執行階段回報的值可能含有 `GameInput.h` 沒有定義的位元。實測 Xbox One 控制器回報 `0x01040007`，其中 `0x01000000` 不屬於 API 版本 3 的 `GameInputKind`（舊版 v0 API 曾把此位元定義為 `GameInputKindUiNavigation`）。`GameInputKind` 是旗標列舉，只要有任何一個位元沒有名稱，`ToString()` 就會改印數字。本套件依標頭產生列舉並原樣保留原生值，不會自行補上或清除未定義的位元。

處理方式：

1. 以位元檢查判斷支援的輸入種類，例如 `(info.SupportedInput & GameInputKind.GameInputKindGamepad) != 0` 或 `info.SupportedInput.HasFlag(GameInputKind.GameInputKindGamepad)`，不要用 `==` 比對整個值。
2. 需要顯示名稱時，先以已知種類遮罩再轉字串，例如 `(info.SupportedInput & ~(GameInputKind)0x01000000).ToString()`，或只列出關心的種類。

## 物件生命週期與執行緒

常見症狀：

- 呼叫 `GameInputDevice`、`GameInputReading` 等物件時拋出 `GameInputException`，訊息為「GameInput 原生物件已不再存在」。

說明與處理方式：

1. 所有包裝型別的 `Dispose()` 都可重複、並行呼叫，只有第一次會釋放原生參考。在 `DeviceChanged` 等原生回呼中 Dispose `GameInputClient` 或 `GameInputDeviceManager` 是安全的：解除註冊會在背景完成，不會卡住回呼執行緒。`GameInputClient.Dispose()` 會等其他執行緒上已在進行中的原生呼叫返回後，才釋放原生物件。
2. 沒有呼叫 `Dispose()` 的包裝會在 GC 回收時由 `SafeHandle` 終結器釋放原生參考，但回收時機不確定，仍應以 `using` 或 `Dispose()` 明確釋放。GameInput 根物件釋放後，從它取得的裝置、reading 等子物件都會失效。請先釋放子物件，最後才釋放 `GameInputClient` 或 `GameInputDeviceManager`；需要保留的資料請先轉成 snapshot。
3. 所有目標框架都透過 vtable 函式指標呼叫 GameInput，不使用 COM Interop 的 RCW，所以 GameInput 物件不受 COM apartment 限制：在 STA（WinForms、WPF 的 UI 執行緒）建立的物件可以在背景執行緒使用，反之亦然。
4. 背景程式或主控台程式收不到輸入時，請確認焦點政策：GameInput 預設只把輸入交給前景應用程式，需要背景輸入時請呼叫 `SetFocusPolicy(GameInputFocusPolicy.GameInputEnableBackgroundInput)`。另外，實測 GameInput 3.5.274 與 3.5.278 都收不到以 `SendInput` 模擬的鍵盤輸入（虛擬鍵與掃描碼兩種方式皆然），自動化測試請改用實體裝置。

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

目前 `net8.0` 與 `net10.0` 路徑都已實際跑過 `dotnet publish -p:PublishAot=true` 端對端驗證，涵蓋裝置列舉、非同步 API、事件、依賴注入與主要 snapshot 路徑。CI 也會以 `tests/InputWeave.GameInput.AotSmoke` 對兩個目標框架執行 NativeAOT 發佈並執行產生的原生檔；CI 代理程式沒有 GameInput 執行階段時，只驗證受控載入器與探測路徑。

仍需注意：

- `.csproj` 只對 `net8.0` 與 `net10.0` 宣告 `IsAotCompatible`，建置時會以 trim／AOT 分析器檢查相容性；`net48` 不適用。
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
