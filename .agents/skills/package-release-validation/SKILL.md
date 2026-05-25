---
name: package-release-validation
description: Use when validating NuGet packaging, release readiness, redist documentation, or VS2026/.NET build checks.
---

使用此 skill 時：

1. 執行 `dotnet restore`、`dotnet build InputWeave.GameInput.slnx -c Release`、`dotnet test InputWeave.GameInput.slnx -c Release`。
2. 執行 `dotnet format InputWeave.GameInput.slnx --verify-no-changes`。
3. 執行 `pwsh ./eng/Verify-GameInputBindings.ps1` 與 `pwsh ./eng/Verify-GameInputCoverage.ps1`。
4. 執行 `pwsh ./eng/Validate-TextEncoding.ps1` 與 `pwsh ./eng/Validate-AgentDocs.ps1`。
5. 執行 `dotnet pack src/InputWeave.GameInput/InputWeave.GameInput.csproj -c Release -o .tmp/packages`。
6. 確認 wrapper NuGet 版本為 `0.0.1`，release/tag 名稱為 `v0.0.1`，license metadata 為 `CC0-1.0`。
7. 確認 nupkg 不包含 `GameInputRedist.msi`、`GameInputRedist.dll` 或 native shim。
8. 發佈前重跑 NuGet ID、GitHub 與商標名稱檢查，避免與既有套件或品牌衝突。
