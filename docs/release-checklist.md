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
pwsh ./eng/Validate-TextEncoding.ps1
pwsh ./eng/Validate-AgentDocs.ps1
dotnet pack src/InputWeave.GameInput/InputWeave.GameInput.csproj -c Release -o .tmp/packages
```

## 套件檢查

- `.nupkg` 檔名應為 `InputWeave.GameInput.0.0.1.nupkg`。
- `.nupkg` 不得包含 `GameInputRedist.msi`、`GameInputRedist.dll` 或 `InputWeave.GameInput.Native.dll`。
- `.csproj` 不得宣告 `IsAotCompatible` 或 `IsTrimmable`，release workflow 不得新增 NativeAOT、trimming 或 single-file 發佈矩陣。
- `README.md` 必須包含 GameInput 可轉散發套件的安裝責任說明。
- `README.md` 必須明確說明目前不宣告 NativeAOT、trimming 或 single-file 發佈相容性。
- `README.md` 必須連到 `docs/gameinput-cookbook.md`。
- 常見情境指南不得暗示包裝套件會散佈 `GameInputRedist.msi`、`GameInputRedist.dll` 或原生橋接 DLL。
- 常見情境指南不得暗示目前支援 NativeAOT、trimming 或 single-file。
- 可轉散發套件文件必須說明受控載入器與 Microsoft C++ 載入器的行為對齊、DLL 劫持防護邊界與 `GameInputRuntime.TryProbe` 診斷方式。
- 可轉散發套件文件不得暗示包裝套件會散佈 `GameInputRedist.msi`、`GameInputRedist.dll` 或原生橋接 DLL。
- `docs/gameinput-api-coverage.md` 必須標示缺口為 0。
- `InputWeave.GameInput.xml` 必須包含 public/protected API 的 `summary`、`param` 與 `returns`；`dotnet test` 會驗證 XML 文件完整性。

## 硬體抽測

有實體裝置與 GameInput 執行階段的機器可加跑：

```powershell
$env:INPUTWEAVE_GAMEINPUT_HARDWARE_TESTS = '1'
dotnet test InputWeave.GameInput.slnx -c Release --filter Hardware
```

硬體測試環境不足時，不得以人工假通過取代；應記錄為未執行。
