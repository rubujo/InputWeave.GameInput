---
name: gameinput-binding-generation
description: 當需要從 Microsoft GameInput.h 重產 C# 互通層繫結，或修改繫結產生器時使用。
---

1. 先核對 Directory.Packages.props 與 eng/gameinput-baseline.json。預設使用 API 4 主標頭；不要恢復 v3 介面、舊狀態列舉或指標容器。
2. 產生邏輯只修改 tools/InputWeave.GameInput.BindingsGenerator；XML 文件來源只修改 eng/gameinput-xml-docs.json，不手改產生檔或雜湊。
3. 用 pwsh ./eng/Update-GameInputVersion.ps1 -Version <版本> 重產；只有明確遷移 API 時才指定 -ApiVersion。產生式程式碼必須為 File-scoped Namespace、UTF-8 無 BOM、CRLF，且包含英語／正體中文完整 XML 文件，不得產生 pragma 壓制警告。
4. net48、net8.0、net10.0 共用裸 vtable 結構與 delegate* unmanaged[Stdcall] 投影，不使用 RCW、ComWrappers 或原生橋接。前三個 slot 是 QueryInterface、AddRef、Release。
5. C++ bool 欄位用 UnmanagedType.I1，裸 vtable 的 bool 回傳值與引數用 byte。固定傳入的結構必須驗證 managed/native 大小與欄位位移；特別注意 API 4 三位元組 RGB 結構與觸控接觸點。
6. 所有原生呼叫均持有 ComLease；COM out 參數是自有參考，借用回呼指標要先 AddRef 再包裝。兩種參考都必須確定性釋放。net48 回呼以靜態委派保存生命週期，現代 TFM 用 UnmanagedCallersOnly。
7. 裝置資訊以 API 4 getter 複製，不暴露原生指標。字串、陣列、觸控表面都要有數量上限、回傳長度檢查與有界重試；含 ByValArray／ByValTStr 的 Haptic 結構應使用明確封送緩衝區。
8. API 4 裝置事件非同步派送，沒有同步列舉完成訊號。不得沿用 v3 enumerationKind 或虛構原生前後狀態；探索時間窗與限制需同步記載於 XML 文件與遷移文件。
9. 更新 eng/gameinput-wrapper-coverage.json 與 docs/gameinput-api-coverage.md，跑 Verify-GameInputBindings.ps1（雜湊、重產、IID、slot 與獨立原生簽章核對）及 Verify-GameInputCoverage.ps1（逐方法呼叫與文件數量核對）。
10. 三框架 build / test、format、文字編碼與 XML 文件測試必須通過。保留假 COM 生命週期、API 4 燈光／觸控緩衝區與結構配置測試。真實 runtime 測試需確認載入版本支援 API 4；E_NOINTERFACE 不可當作硬體未連線而跳過。
11. 有使用者授權的硬體時啟用 INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS=1，分別記載真實 API 4、假 COM、裝置特定功能與 NativeAOT 證據。Xbox 手把不能代表 DualSense、燈光或觸控已實測。
