---
name: gameinput-version-update
description: 當需要更新 Microsoft.GameInput 版本、產生式繫結、基準雜湊或覆蓋率文件時使用。
---

使用此技能時：

1. 先讀取 `Directory.Packages.props` 與 `eng/gameinput-baseline.json`。
2. 執行 `pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated` 確認目前版本是否落後。
3. 執行 `pwsh ./eng/Show-GameInputGitHubReleases.ps1` 交叉核對 [microsoftconnect/GameInput](https://github.com/microsoftconnect/GameInput) 的 GitHub Releases。GitHub Release tag 版號（例如 `v3.3.195.0`）與 NuGet 套件版號（例如 `3.5.270`）採不同編號機制，不能直接比對版本字串；NuGet 套件說明通常只彙整某個系列的重點，可能不含 GitHub Release Notes 裡逐版列出的細節（例如特定功能新增或 bug 修正）。人工核對輸出內容，確認是否有 NuGet 說明沒提到、但 `eng/gameinput-version-notes.json` 等文件應該補上的異動。
4. 若需要追版，執行 `pwsh ./eng/Update-GameInputVersion.ps1`，讓腳本更新 NuGet 版本、基準雜湊、產生式互通層與 ABI 資訊清單。
5. 不要手動編輯產生檔或雜湊值；若輸出不正確，修正更新腳本或產生器後重跑。
6. 依 NuGet 套件 README 與步驟 3 核對到的 GitHub Release Notes 更新 `eng/gameinput-version-notes.json` 的正體中文摘要，並確認 `docs/gameinput-version-report.md`、`docs/gameinput-redist.md` 與 `docs/gameinput-api-coverage.md` 同步更新。
7. 完成步驟 6 的核對後，執行 `pwsh ./eng/Show-GameInputGitHubReleases.ps1 -MarkReviewed` 將 `eng/gameinput-github-release-tracking.json` 標記為已核對到最新 Release，避免下次誤判為未核對。
8. 執行 `pwsh ./eng/Verify-GameInputBindings.ps1` 與 `pwsh ./eng/Verify-GameInputCoverage.ps1`。
9. 執行 `dotnet build InputWeave.GameInput.slnx -c Release`、`dotnet test InputWeave.GameInput.slnx -c Release` 與 `pwsh ./eng/Validate-TextEncoding.ps1`。
