---
name: package-release-validation
description: 當需要驗證 NuGet 包裝、發佈前狀態、可轉散發套件文件或 VS2026/.NET 建置檢查時使用。
---

使用此技能時：

1. 執行 `dotnet restore`、`dotnet build InputWeave.GameInput.slnx -c Release`、`dotnet test InputWeave.GameInput.slnx -c Release`。
2. 執行 `dotnet format InputWeave.GameInput.slnx --verify-no-changes`。
3. 執行 `pwsh ./eng/Verify-GameInputBindings.ps1`、`pwsh ./eng/Verify-GameInputCoverage.ps1` 與 `pwsh ./eng/Verify-DocSnippets.ps1`（文件 C# 範例必須能實際編譯）。
4. 執行 `pwsh ./eng/Validate-TextEncoding.ps1` 與 `pwsh ./eng/Validate-AgentDocs.ps1`。
5. 執行 `dotnet pack src/InputWeave.GameInput/InputWeave.GameInput.csproj -c Release -o .tmp/packages`；`EnablePackageValidation` 會在 pack 時比對 `net8.0` 與 `net10.0` 的公開 API，出現 `CP` 開頭錯誤時修正程式碼，不要產生隱藏檔略過。
6. 確認包裝套件版本為 `0.0.1`，發佈標籤名稱為 `v0.0.1`，授權中繼資料為 `CC0-1.0`。
7. 確認 `.nupkg` 只包含 `lib/net48`、`lib/net8.0`、`lib/net10.0` 三組 `InputWeave.GameInput.dll` 與 `.xml`，且不包含 `GameInputRedist.msi`、`GameInputRedist.dll` 或原生橋接 DLL。
8. 發佈前重跑 NuGet ID、GitHub 與商標名稱檢查，避免與既有套件或品牌衝突。

## API 4 驗證

- 使用 docs/gameinput-api4-migration.md 核對破壞性變更；Sample、文件 C# 範例與 AOT Smoke 都必須採 API 4，不能殘留已移除的 v3 呼叫。
- 確認全部 NuGet 相依套件為目前最新穩定版本，並記載核對日期。
- 宣告覆蓋率、假 COM、真實 runtime、特定硬體與 NativeAOT 結果分開記載於 docs/gameinput-api4-validation.md。缺硬體不得改寫為已通過。
- 允許真實硬體驗證時啟用 INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS=1；Xbox USB 特定測試另需 INPUTWEAVE_GAMEINPUT_XBOX_USB_TESTS=1。確認三框架測試有實際執行，不能以 Inconclusive 當作成功驗收。
- NativeAOT 需對 net8.0 與 net10.0 各自 publish 並執行產物；不能把舊 API 3 的硬體證據當作 API 4 當次驗證。
