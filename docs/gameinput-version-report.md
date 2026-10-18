# GameInput 版本報告

目前基準版本：`Microsoft.GameInput` `4.0.300`

目前包裝程式庫版本：`InputWeave.GameInput v0.0.1`，NuGet / MSBuild 版本為 `0.0.1`。

- 包裝 API 版本：`4`（套件主標頭 API 版本：`4`）
- NuGet 套件 SHA256：`AE7DC3A311466A72DDCFD6C5CCD5B1E917BE890658236FA9791E937115AC2D79`
- `native/include/GameInput.h` SHA256：`09B43DB68763D29CACBFEDCD4674ED7EF443ED2380A0BF20B2C8B452545D49A0`
- `redist/GameInputRedist.msi` SHA256：`158505DFA70B082447371D402C2F50EB61973B9BF227AF23CE682CC90D70E453`

低階互通層來源：`src/InputWeave.GameInput/Interop/Generated/` 下的列舉、常數、HRESULT、IID、回呼委派、結構配置、COM 介面與 `gameinput-abi-manifest.json` 均由目前基準的 `native/include/GameInput.h` 產生。套件追版預設維持目前包裝 API 範圍；明確遷移時使用 `-ApiVersion` 指定版本，並完成產生器、受控包裝與逐方法覆蓋率驗證。

## Microsoft 官方 4.0 版本異動摘要

Microsoft 的套件 README 以 `4.0` 系列彙整版本說明，未提供 `4.0.300` 的逐 build Changelog。以下為本專案依官方內容整理的正體中文摘要：

- 新增裝置燈光、觸控裝置與 Nintendo Switch Pro 控制器支援。
- 新增查詢裝置 XInput 索引的支援，以及軸、主要／次要開火與背鍵標籤欄位。
- GameInputArcadeStickInfo 新增 LSB／RSB 按鈕旗標與對應標籤。
- NuGet 套件新增 Windows 與 Xbox 的並存部署二進位檔，並提供有限的 x86 架構支援。
- 將多種 IGameInputDevice 回呼整併為單一事件通知，並以 IGameInputDevice getter 取代 GameInputDeviceInfo 與相關結構。
- 改善遠端存取應用程式的裝置支援與 HID 裝置震動支援，並包含穩定性與效能改善。
- 包裝程式庫已遷移至 API 4，使用 native/include/GameInput.h 主標頭並更新裝置 getter、統一事件、燈光、觸控與功能報告 API。
- 已交叉核對 GitHub 最新 v3.3.195.0 Release Notes；其中背鍵、DualSense 陀螺儀、PNP 容器分組、ARM 支援、Elite 重複裝置修正、終端機控制器支援與 CFG 改善均屬既有歷史異動，並非 4.0 新增功能。

來源：[Microsoft.GameInput 4.0.300](https://www.nuget.org/packages/Microsoft.GameInput/4.0.300)

## 追版流程

1. 執行 `pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated` 確認 NuGet 是否有新版。
2. 執行 `pwsh ./eng/Show-GameInputGitHubReleases.ps1` 交叉核對 [microsoftconnect/GameInput](https://github.com/microsoftconnect/GameInput) 的 GitHub Releases——GitHub tag 版號與 NuGet 版號編號機制不同，NuGet 套件說明可能未列出 Release Notes 上的細節，需人工核對後視需要更新 `eng/gameinput-version-notes.json`，再執行 `-MarkReviewed` 標記。
3. 若有新版，執行 `pwsh ./eng/Update-GameInputVersion.ps1`。
4. 檢查 `Directory.Packages.props`、`eng/gameinput-baseline.json`、`eng/gameinput-version-notes.json`、`src/InputWeave.GameInput/Interop/Generated/` 下的 `.g.cs`、`gameinput-abi-manifest.json` 與本報告。
5. 執行 `dotnet build`、`dotnet test`、`pwsh ./eng/Verify-GameInputBindings.ps1`、`pwsh ./eng/Verify-GameInputCoverage.ps1`。
6. 若 GameInput.h 公開 API 有新增或異動，先更新產生器映射，再更新覆蓋率與版本文件。
