---
name: package-release-validation
description: 當任務涉及 NuGet 包裝、發佈前檢查、redist 說明或 VS2026/.NET 建置驗證時使用。
---

使用此 skill 時：

1. 執行 `dotnet restore`、`dotnet build InputWeave.GameInput.slnx -c Release`、`dotnet test InputWeave.GameInput.slnx -c Release`。
2. 執行 `dotnet format InputWeave.GameInput.slnx --verify-no-changes`。
3. 執行 `pwsh ./eng/Verify-GameInputBindings.ps1` 與 `pwsh ./eng/Verify-GameInputCoverage.ps1`。
4. 執行 `pwsh ./eng/Validate-TextEncoding.ps1` 與 `pwsh ./eng/Validate-AgentDocs.ps1`。
5. 確認 wrapper NuGet 版本為 `0.0.1`，release/tag 名稱為 `v0.0.1`，且沒有包入 `GameInputRedist.msi`。
6. 發佈前重跑 NuGet ID、GitHub 與商標名稱檢查，避免與既有套件或品牌衝突。
