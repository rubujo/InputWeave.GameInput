---
name: gameinput-version-update
description: 當需要更新 Microsoft.GameInput 版本、產生式繫結、基準雜湊或覆蓋率文件時使用。
---

使用此技能時：

1. 先讀取 `Directory.Packages.props` 與 `eng/gameinput-baseline.json`。
2. 執行 `pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated` 確認目前版本是否落後。
3. 若需要追版，執行 `pwsh ./eng/Update-GameInputVersion.ps1`，讓腳本更新 NuGet 版本、基準雜湊、產生式互通層與 ABI 資訊清單。
4. 不要手動編輯產生檔或雜湊值；若輸出不正確，修正更新腳本或產生器後重跑。
5. 確認 `docs/gameinput-version-report.md`、`docs/gameinput-redist.md` 與 `docs/gameinput-api-coverage.md` 同步更新。
6. 執行 `pwsh ./eng/Verify-GameInputBindings.ps1` 與 `pwsh ./eng/Verify-GameInputCoverage.ps1`。
7. 執行 `dotnet build InputWeave.GameInput.slnx -c Release`、`dotnet test InputWeave.GameInput.slnx -c Release` 與 `pwsh ./eng/Validate-TextEncoding.ps1`。
