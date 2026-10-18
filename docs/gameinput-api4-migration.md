# API 4 遷移

目前基準為 Microsoft.GameInput 4.0.300 / API 4，支援 net48、net8.0、net10.0。這次變更會破壞 API 3 呼叫端相容性，不保留 v3 COM 投影、舊狀態列舉、列舉模式或裝置資訊指標容器。

| API 3 使用方式 | API 4 替代方式 |
| --- | --- |
| GameInputDeviceInfo / GetDeviceInfo | 個別裝置 getter，或 GetDeviceInfoSnapshot 的純受控資料 |
| snapshot.Native | snapshot.Identity；不包含任何原生指標 |
| GameInputDeviceStatus / device.Status | device.IsConnected() 與 GameInputDeviceEvent |
| statusFilter / enumerationKind | 統一裝置事件；需要篩選時依事件種類處理 |
| 前後裝置狀態與原生事件 timestamp | DeviceEvent；manager.Timestamp 是受控處理常式收到事件時取得的時間戳記 |
| SetFocusPolicy | EnableBackgroundInput(bool) |
| System Button callback | Reading 的 TryGetSystemButtonState |
| Keyboard Layout callback | GameInputDeviceEventKeyboardLayoutUpdated，再讀取 GetKeyboardInfo |

裝置能力可能異動，GetDeviceInfoSnapshot 每次重新複製 getter 結果，不快取 XInput 索引或鍵盤配置。集合包含 FeatureReports 與 TouchSurfaces，值相等性與雜湊計算會逐一比較內容。

API 4 的 RegisterDeviceCallback 非同步派送初始裝置事件，沒有同步列舉完成訊號。EnumerateDevices / RefreshDevices 是便利探索快照：收集固定的 1.25 秒時間窗；可能晚於時間窗出現的新裝置應透過持續事件訂閱取得。兩次探索不保證相同集合或順序。不得從原生回呼執行緒同步探索裝置；需要時使用背景非同步方法。

WaitForDeviceEventAsync 直接遵循原生 API 4 事件語意，包含新註冊對既有裝置派送的初始 Connected 通知；不模擬 v3 的 NoEnumeration 模式。需要持續處理連線與中繼資料異動時，應持有一個 RegisterDeviceCallback 註冊並依應用程式需求追蹤裝置 ID。回呼借用裝置僅在處理常式執行期間有效，跨執行緒或延後處理時請複製快照。

API 4 新增燈光群組、觸控表面、XInput 索引及 raw feature report 的 Get / Set。一般燈光請使用 SetLightingColor、ApplyLightingColors、ApplyLightingSystemDefault；裝置是否支援需先查詢能力。

需要安裝支援 API 4 的 GameInput runtime。只找到舊 DLL 或 GameInputInitialize 進入點，不代表該 runtime 支援 API 4 IID；舊 runtime 可能回報 E_NOINTERFACE。
