---
name: gameinput-version-tracking
description: Use when checking, updating, or confirming Microsoft.GameInput NuGet versions and GameInputRedist hashes.
---

使用此 skill 時：

1. 先讀取 `Directory.Packages.props` 與 `eng/gameinput-baseline.json`。
2. 執行 `pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated` 確認目前版本是否落後。
3. 若需要追版，執行 `pwsh ./eng/Update-GameInputVersion.ps1`。
4. 檢查 `docs/gameinput-version-report.md` 與 `docs/gameinput-api-coverage.md` 是否同步更新。
5. 執行 `pwsh ./eng/Verify-GameInputCoverage.ps1` 確認 v0.0.1 coverage 沒有缺口。
6. 不要手動編輯產生檔或雜湊值；讓腳本產生。
