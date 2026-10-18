# API 4 驗證紀錄

驗證日期：2026-10-18（Asia/Taipei）。對象為本次 API 4 工作目錄變更；套件版本維持 InputWeave.GameInput 0.0.1，尚未發布。

## 來源與基準

- Microsoft.GameInput：4.0.300，包裝 API：4，使用 native/include/GameInput.h 主標頭。
- SDK：10.0.402；VS2026 Developer Command Prompt：18.11.0。
- 本機已依使用者明確授權安裝 Microsoft 官方 GameInput 4.0.300 MSI；安裝前 Microsoft 簽章有效，MSI SHA256 與 eng/gameinput-baseline.json 一致。安裝結束碼為 0，未重開機。
- 最後 fetch 後 main 與 origin/main 的提交差異為 0 / 0，無需變基。

## 三框架測試

啟用 INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS=1 與 INPUTWEAVE_GAMEINPUT_XBOX_USB_TESTS=1 後執行 Release 全方案測試。

| 目標框架 | 通過 | 失敗 | 略過 |
| --- | ---: | ---: | ---: |
| net48 | 181 | 0 | 0 |
| net8.0 | 187 | 0 | 0 |
| net10.0 | 187 | 0 | 0 |
| 合計 | 555 | 0 | 0 |

TRX 留存於 .tmp/api4-final。測試包含假 COM 參考計數、並行釋放、回呼解除註冊、API 4 結構配置、三位元組 RGB 傳值、觸控緩衝區、系統按鈕、XInput 索引、字串／陣列長度異常與重試、快照集合相等性，以及真實 runtime 路徑。

## 宣告與 ABI 覆蓋率

- 列舉：27 / 27；結構：33 / 33；回呼：2 / 2；COM 介面：7 / 7；HRESULT：10 / 10。
- COM 方法：116 / 116，原生函式：2 / 2（GameInputInitialize 與 inline GameInputCreate 由 GameInputClient.Create 承接），常數：21 / 21。
- 缺口：0。主標頭、基準、ABI manifest、逐方法包裝清單與產生檔核對通過；獨立原生簽章核對涵蓋 slot 順序、回傳值及引數的 ABI 型別。
- VS2026 C++ x64 直接編譯官方標頭，再與 net10.0 的 Marshal.SizeOf / Marshal.OffsetOf 比較，33 個結構共 280 項大小及欄位位移全部一致，包含 force feedback 的具名 union。探測來源與輸出留存於 .tmp/api4-layout.cpp、.tmp/api4-native-layout.txt、.tmp/api4-layout-managed。
- 產生器拒絕 API 3 標頭，且拒絕前不寫入產生檔；不保留 API 3 原生介面、舊狀態列舉或裝置資訊指標容器。

## USB Xbox One 驗收

實際探索到 Xbox One 控制器（VID 045E / PID 02EA）。三框架均驗證連線、名稱與 PnP 路徑、裝置 getter、gamepad 能力與 reading、控制器標籤、raw report 描述及 XInput 索引。TRX 內硬體測試輸出包含實際 runtime 檔案版本 4.0.300.0。

此次未要求人工逐一按鍵，未把靜態 reading 成功當作所有按鈕變化或輸出效果的實測。沒有 DualSense / Edge、燈光或觸控硬體；對應 ABI 與資料傳遞由假 COM、緩衝區測試及原生配置比對驗證。

## NativeAOT 與 Sample

- net8.0 / win-x64 與 net10.0 / win-x64 均成功 publish 並執行 NativeAOT 產物。
- 兩者皆確認 NativeAOT=True，收到 API 4 初始 Connected 事件，列舉到三個裝置（含 Xbox One），完成 gamepad 讀取、快照相等性與 DI 管理器路徑。
- 產物位於 .tmp/api4-aot-final/net8.0 與 .tmp/api4-aot-final/net10.0。
- Sample 已採 API 4 統一回呼與 getter，並顯示 XInput、燈光群組、觸控表面及 feature report 能力。預設不觸發震動或燈光輸出。
- 文件共 15 組 C# 範例依 TFM 標記或預設三框架完成編譯。

## NuGet 穩定版本與包裝

已向 NuGet flat-container 索引重新核對全部集中管理套件，排除預覽版。

| 套件 | 最新穩定版本 |
| --- | --- |
| BenchmarkDotNet | 0.15.8 |
| Microsoft.Extensions.DependencyInjection | 10.0.13 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.13 |
| Microsoft.GameInput | 4.0.300 |
| Microsoft.NETFramework.ReferenceAssemblies.net48 | 1.0.3 |
| Microsoft.SourceLink.GitHub | 10.0.402 |
| MSTest | 4.5.1 |

Release pack 與跨框架公開 API 驗證通過。.tmp/api4-packages/InputWeave.GameInput.0.0.1.nupkg 含三框架 DLL/XML，沒有 GameInputRedist.msi、GameInputRedist.dll 或原生橋接 DLL。

## 維護規範與限制

AGENTS.md 與既有三個技能已同步 API 4、三框架、runtime 授權與驗證界線；沒有新增重複技能。XML 文件來源已移除不再使用的 v3 條目。格式、文字編碼、Agent 文件與差異空白檢查通過。

宣告零缺口不代表每種硬體功能皆已實測。裝置探索使用明確 1.25 秒收集時間窗，不宣稱原子列舉；WaitForDeviceEventAsync 遵循 API 4，包含新註冊的初始 Connected 通知。破壞性變更見 gameinput-api4-migration.md；DualSense companion helper 的採用評估見 gameinput-dualsense-helper.md。
