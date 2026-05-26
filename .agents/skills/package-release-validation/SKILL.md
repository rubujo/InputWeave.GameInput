---
name: package-release-validation
description: 當需要驗證 NuGet 包裝、發佈前狀態、可轉散發套件文件或 VS2026/.NET 建置檢查時使用。
---

使用此技能時：

1. 執行 `dotnet restore`、`dotnet build InputWeave.GameInput.slnx -c Release`、`dotnet test InputWeave.GameInput.slnx -c Release`。
2. 執行 `dotnet format InputWeave.GameInput.slnx --verify-no-changes`。
3. 執行 `pwsh ./eng/Verify-GameInputBindings.ps1` 與 `pwsh ./eng/Verify-GameInputCoverage.ps1`。
4. 執行 `pwsh ./eng/Validate-TextEncoding.ps1` 與 `pwsh ./eng/Validate-AgentDocs.ps1`。
5. 執行 `dotnet pack src/InputWeave.GameInput/InputWeave.GameInput.csproj -c Release -o .tmp/packages`。
6. 確認包裝套件版本為 `0.0.1`，發佈標籤名稱為 `v0.0.1`，授權中繼資料為 `CC0-1.0`。
7. 確認 `.nupkg` 不包含 `GameInputRedist.msi`、`GameInputRedist.dll` 或原生橋接 DLL。
8. 發佈前重跑 NuGet ID、GitHub 與商標名稱檢查，避免與既有套件或品牌衝突。
