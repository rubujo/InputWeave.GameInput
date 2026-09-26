# InputWeave.GameInput v0.0.1 發佈檢查表

`v0.0.1` 是 Git 標籤 / GitHub Release 名稱；NuGet / MSBuild 套件版本為 `0.0.1`。

## 必跑命令

```powershell
dotnet restore InputWeave.GameInput.slnx
dotnet build InputWeave.GameInput.slnx -c Release
dotnet test InputWeave.GameInput.slnx -c Release
dotnet format InputWeave.GameInput.slnx --verify-no-changes
pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated
pwsh ./eng/Verify-GameInputBindings.ps1
pwsh ./eng/Verify-GameInputCoverage.ps1
pwsh ./eng/Verify-DocSnippets.ps1
pwsh ./eng/Validate-TextEncoding.ps1
pwsh ./eng/Validate-AgentDocs.ps1
dotnet pack src/InputWeave.GameInput/InputWeave.GameInput.csproj -c Release -o .tmp/packages
```

## 套件檢查

- `.nupkg` 檔名應為 `InputWeave.GameInput.0.0.1.nupkg`。
- `.nupkg` 必須只包含 `lib/net48`、`lib/net8.0`、`lib/net10.0` 三組 `InputWeave.GameInput.dll` 與 `.xml`；pack 時由 `EnablePackageValidation` 檢查相容目標框架的公開 API 一致，CI 與 release workflow 都會執行。
- `.nupkg` 不得包含 `GameInputRedist.msi`、`GameInputRedist.dll` 或 `InputWeave.GameInput.Native.dll`。
- `.csproj` 只對 `net8.0` 以上相容的 TFM（目前為 `net8.0` 與 `net10.0`）宣告 `IsAotCompatible`，讓 trim／AOT 分析器在建置期強制檢查；`net48` 不宣告，也不另外宣告 `IsTrimmable`。CI 以 `tests/InputWeave.GameInput.AotSmoke` 對 `net8.0` 與 `net10.0` 實際執行 NativeAOT 發佈與煙霧測試；release workflow 不得新增 NativeAOT、trimming 或 single-file 發佈矩陣。
- `README.md` 必須包含 GameInput 可轉散發套件的安裝責任說明。
- `README.md` 必須如實描述 NativeAOT 驗證狀態（`net8.0` 與 `net10.0` 已實測 `dotnet publish -p:PublishAot=true` 端對端驗證），且不得宣告 single-file 發佈相容性。
- `README.md` 必須連到 `docs/gameinput-cookbook.md`。
- 常見情境指南不得暗示包裝套件會散佈 `GameInputRedist.msi`、`GameInputRedist.dll` 或原生橋接 DLL。
- 常見情境指南的 NativeAOT 敘述必須與 README 一致（已實測驗證），且不得暗示支援 single-file。
- 可轉散發套件文件必須說明受控載入器與 Microsoft C++ 載入器的行為對齊、DLL 劫持防護邊界與 `GameInputRuntime.TryProbe` 診斷方式。
- 可轉散發套件文件不得暗示包裝套件會散佈 `GameInputRedist.msi`、`GameInputRedist.dll` 或原生橋接 DLL。
- `docs/gameinput-version-report.md` 必須包含目前 Microsoft.GameInput minor 系列的官方版本異動摘要與來源連結。
- `docs/gameinput-api-coverage.md` 必須標示缺口為 0。
- `README.md` 與 `docs/*.md` 內的每個 C# 範例都必須是可獨立編譯的頂層程式，並通過 `eng/Verify-DocSnippets.ps1`。
- `InputWeave.GameInput.xml` 必須包含 public/protected API 的 `summary`、`param` 與 `returns`；`dotnet test` 會驗證 XML 文件完整性。

## 硬體抽測

有實體裝置與 GameInput 執行階段的機器可加跑：

```powershell
$env:INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS = '1'
dotnet test InputWeave.GameInput.slnx -c Release --filter Hardware
```

硬體測試環境不足時，不得以人工假通過取代；應記錄為未執行。
