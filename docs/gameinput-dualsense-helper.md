# DualSense companion helper 評估

核對日期：2026-10-18。來源為 Microsoft GameInput 官方儲存庫的 [README](https://github.com/microsoftconnect/GameInput/blob/8bc8507a2dc8802e63695aac4f85ce775f939d37/companion/DualSense/README.md) 與 [DualSenseHelper.h](https://github.com/microsoftconnect/GameInput/blob/8bc8507a2dc8802e63695aac4f85ce775f939d37/companion/DualSense/DualSenseHelper.h)，目前版本已明確標示 MIT 授權。

決策：本次不直接引入 C++ helper 或原生橋接 DLL。通用燈光優先使用 API 4；需要 DualSense 專屬功能時，以既有受控 raw report API 作為後續擴充入口。這項 companion 範例不屬於 GameInput.h 的 API 4 宣告覆蓋率分母。

理由與界線：

- helper 提供光條、玩家 LED、震動、麥克風靜音 LED 與自適應扳機的原始輸出報告。
- 它是 header-only C++ 範例，目前原始碼仍硬編碼 GameInput::v3 型別，不可直接作為 C# API 4 繫結引用，也不應為此重新引入 v3 COM 介面。
- 它組合 64 位元組 USB 格式報告，報告 ID 為 0x02；不處理 Bluetooth framing、checksum、輸入解析或連線管理。VID/PID 只能辨識 DualSense / Edge 機型，不能證明使用 USB 傳輸。
- API 4 已提供通用燈光與觸控能力，先使用這些能力可降低對裝置特定 HID 格式的相依。
- 目前驗收硬體是 USB Xbox One 控制器，沒有 DualSense / Edge。沒有對應硬體前，不宣告 DualSense 專屬輸出已實測或支援 Bluetooth。

如未來加入純 C# helper，應獨立保存上游 commit、MIT 著作權與授權文字，明確限制 USB 傳輸，並先驗證每種報告的長度、旗標、扳機區塊位移、錯誤傳遞與 COM 釋放，再以 DualSense / Edge USB 實機驗收。既有 CreateRawDeviceReport、SetRawData、SendRawDeviceOutput 已可承接該設計，不需要原生橋接。
