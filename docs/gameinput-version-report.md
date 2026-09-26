# GameInput 版本報告

目前基準版本：`Microsoft.GameInput` `3.5.278`

目前包裝程式庫版本：`InputWeave.GameInput v0.0.1`，NuGet / MSBuild 版本為 `0.0.1`。

- API 版本：`3`
- NuGet 套件 SHA256：`52837899F671D195DF04E27F349365B98637C6CD040F4CECD69598EF7889FF03`
- `native/include/GameInput.h` SHA256：`FBB769BEF01B133DBB62E3622A7137482CB52B642EA21DE080CB98279EF9610F`
- `redist/GameInputRedist.msi` SHA256：`25300B9B4DA0BE4260DF8F84539DA068483804B0871FE5C701A81312CF4E0FFE`

低階互通層來源：`src/InputWeave.GameInput/Interop/Generated/` 下的列舉、常數、HRESULT、IID、回呼委派、結構配置、COM 介面與 `gameinput-abi-manifest.json` 均由目前基準的 `GameInput.h` 產生。

## Microsoft 官方 3.5 版本異動摘要

Microsoft 的套件 README 以 `3.5` 系列彙整版本說明，未提供 `3.5.278` 的逐 build Changelog。以下為本專案依官方內容整理的正體中文摘要：

- 新增 Agility SDK 樣式的並存部署支援。
- 新增 XInput 與背景 GIP 原始裝置報告支援。
- 新增 PlayStation 5 DualSense 功能的 companion header；上游文件指向 Microsoft GameInput GitHub 儲存庫。
- 公開標頭與靜態程式庫改採 MIT 授權。
- 修正 Xbox 從暫停狀態恢復後遺失輸入的問題。
- 修正 Xbox 輸入裝置缺少顯示名稱的問題。
- 修正部分 Windows.Gaming.Input 遊戲收到重複 DualSense Edge 輸入的問題。
- 包含其他穩定性與效能改善。

來源：[Microsoft.GameInput 3.5.278](https://www.nuget.org/packages/Microsoft.GameInput/3.5.278)

## 追版流程

1. 執行 `pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated` 確認 NuGet 是否有新版。
2. 執行 `pwsh ./eng/Show-GameInputGitHubReleases.ps1` 交叉核對 [microsoftconnect/GameInput](https://github.com/microsoftconnect/GameInput) 的 GitHub Releases——GitHub tag 版號與 NuGet 版號編號機制不同，NuGet 套件說明可能未列出 Release Notes 上的細節，需人工核對後視需要更新 `eng/gameinput-version-notes.json`，再執行 `-MarkReviewed` 標記。
3. 若有新版，執行 `pwsh ./eng/Update-GameInputVersion.ps1`。
4. 檢查 `Directory.Packages.props`、`eng/gameinput-baseline.json`、`eng/gameinput-version-notes.json`、`src/InputWeave.GameInput/Interop/Generated/` 下的 `.g.cs`、`gameinput-abi-manifest.json` 與本報告。
5. 執行 `dotnet build`、`dotnet test`、`pwsh ./eng/Verify-GameInputBindings.ps1`、`pwsh ./eng/Verify-GameInputCoverage.ps1`。
6. 若 GameInput.h 公開 API 有新增或異動，先更新產生器映射，再更新覆蓋率與版本文件。
