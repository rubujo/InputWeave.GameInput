# GameInput 版本報告

目前基準版本：`Microsoft.GameInput` `3.5.268`

目前包裝程式庫版本：`InputWeave.GameInput v0.0.1`，NuGet / MSBuild 版本為 `0.0.1`。

- API 版本：`3`
- NuGet 套件 SHA256：`40C9AD60DA737570C76A909A1238297380F3700CCBF9199AA3DF95E3B88E012B`
- `native/include/GameInput.h` SHA256：`FBB769BEF01B133DBB62E3622A7137482CB52B642EA21DE080CB98279EF9610F`
- `redist/GameInputRedist.msi` SHA256：`49EE9B1A3F8F588075EB596A0288D2ED6D64BF8DD894124E5617B88CAEEC434C`

低階互通層來源：`src/InputWeave.GameInput/Interop/Generated/` 下的列舉、常數、HRESULT、IID、回呼委派、結構配置、COM 介面與 `gameinput-abi-manifest.json` 均由目前基準的 `GameInput.h` 產生。

## Microsoft 官方 3.5 版本異動摘要

Microsoft 的套件 README 以 `3.5` 系列彙整版本說明，未提供 `3.5.268` 的逐 build Changelog。以下為本專案依官方內容整理的正體中文摘要：

- 新增 Agility SDK 樣式的並存部署支援。
- 新增 XInput 與背景 GIP 原始裝置報告支援。
- 新增 PlayStation 5 DualSense 功能的 companion header；上游文件指向 Microsoft GameInput GitHub 儲存庫。
- 公開標頭與靜態程式庫改採 MIT 授權。
- 修正 Xbox 從暫停狀態恢復後遺失輸入的問題。
- 修正 Xbox 輸入裝置缺少顯示名稱的問題。
- 修正部分 Windows.Gaming.Input 遊戲收到重複 DualSense Edge 輸入的問題。
- 包含其他穩定性與效能改善。

來源：[Microsoft.GameInput 3.5.268](https://www.nuget.org/packages/Microsoft.GameInput/3.5.268)

## 追版流程

1. 執行 `pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated` 確認 NuGet 是否有新版。
2. 若有新版，執行 `pwsh ./eng/Update-GameInputVersion.ps1`。
3. 檢查 `Directory.Packages.props`、`eng/gameinput-baseline.json`、`eng/gameinput-version-notes.json`、`src/InputWeave.GameInput/Interop/Generated/` 下的 `.g.cs`、`gameinput-abi-manifest.json` 與本報告。
4. 執行 `dotnet build`、`dotnet test`、`pwsh ./eng/Verify-GameInputBindings.ps1`、`pwsh ./eng/Verify-GameInputCoverage.ps1`。
5. 若 GameInput.h 公開 API 有新增或異動，先更新產生器映射，再更新覆蓋率與版本文件。
