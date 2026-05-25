# AGENTS.md

## 專案定位

`InputWeave.GameInput` 是 Microsoft GameInput 的 C# 分層包裝程式庫。主要目標是同時支援 .NET Framework `net48` 與最新版 `.NET 10` 的 Windows 應用程式，並讓 GameInput API 追版、低階繫結與發佈驗證可維護。

## 建置與測試

- 還原：`dotnet restore`
- 建置：`dotnet build InputWeave.GameInput.slnx -c Release`
- 測試：`dotnet test InputWeave.GameInput.slnx -c Release`
- 格式檢查：`dotnet format InputWeave.GameInput.slnx --verify-no-changes`
- 文字與編碼檢查：`pwsh ./eng/Validate-TextEncoding.ps1`
- Agent 文件檢查：`pwsh ./eng/Validate-AgentDocs.ps1`
- GameInput 繫結檢查：`pwsh ./eng/Verify-GameInputBindings.ps1`
- GameInput coverage 檢查：`pwsh ./eng/Verify-GameInputCoverage.ps1`

## GameInput 追版規則

- 目前基準是 `Microsoft.GameInput` `3.4.218`，API version `3`。
- 修改 interop 前先執行 `pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated`。
- 更新 GameInput 時使用 `pwsh ./eng/Update-GameInputVersion.ps1`，不要手動改產生檔。
- `eng/gameinput-baseline.json` 必須同步記錄 nupkg、`GameInput.h` 與 `GameInputRedist.msi` 的 SHA256。
- `src/InputWeave.GameInput/Interop/Generated/` 下的 `.g.cs` 與 `gameinput-abi-manifest.json` 必須由 generator 產生，用來驗證 enum、constants、HRESULT、IID、struct、callback、XML 文件註解與 COM interface method order。
- Generated interop 的 XML 文件註解來源是 `eng/gameinput-xml-docs.zh-TW.json`；不要手改 generated `.g.cs` 補文件。
- 不要手改 generated interop；若產生檔不符合需求，修改 `tools/InputWeave.GameInput.BindingsGenerator` 後重產。
- `docs/gameinput-api-coverage.md` 的 v0.0.1 coverage 必須維持缺口為 0，release 前需跑 `eng/Verify-GameInputCoverage.ps1`。
- 追版時同步更新 `docs/gameinput-version-report.md`、`docs/gameinput-redist.md` 與 coverage 文件。

## 程式碼規範

- C# 使用 Allman braces、4 空白縮排、每行一個敘述。
- C# 檔案一律使用 file-scoped namespace；新增或重產 `.cs` 時不得回到 block-scoped namespace。
- C# 程式碼必須遵循 VS2026 / `dotnet format` 的 code style 與 analyzer 建議，並配合 `LangVersion latest` 使用 C# 最新穩定語法。
- 若 VS2026 lint 要求可安全套用的新語法，例如 collection expression `[]`、更精準的 overload 或更具體的回傳型別，應更新程式碼而不是壓制規則。
- 不得新增 `#pragma warning disable` 來壓制 C# analyzer 或 VS2026 lint；若出現警告，應修正程式碼、產生器或 `.editorconfig` 規則來源。
- P/Invoke 在 `net10.0-windows` 等現代 TFM 必須使用 `LibraryImport` source generator；`DllImport` 只可存在於 `NETFRAMEWORK` 專用相容檔。
- 本專案預設維持 managed-only wrapper，不導入 native/API shim；除非另有 runtime selection 或載入診斷需求，必須先另行規劃。
- Public API 名稱維持英文技術命名；公開 XML 文件註解使用正體中文台灣用語。
- 所有 public/protected type、member、enum member、delegate、方法參數與非 void 回傳值都必須有 XML 文件註解；不得忽略 `CS1591`。
- `InputWeave.GameInput.Interop` 保留原生 GameInput 識別字，方便對照 Microsoft `GameInput.h`。
- 一般使用者應優先使用 managed wrapper、snapshot、manager、builder 與 safe handle API；低階 interop 是 escape hatch。
- COM 物件與 native reading 必須有明確 `IDisposable` 生命週期。
- 不要把 `GameInputRedist.msi`、`GameInputRedist.dll` 或 native shim 包進 wrapper NuGet。

## Git 提交規範

- Git commit 必須使用約定式提交格式，例如 `feat: 建立 GameInput wrapper`。
- commit 主旨與內文必須使用正體中文台灣用語；type token 依約定式提交維持英文小寫。
- 禁止一行提交；主旨後必須空一行，並撰寫至少一段內文說明變更內容與驗證重點。
- 主旨使用祈使或描述式短句，不加句號；內文可使用段落或條列。
- 若提交包含破壞性變更，必須在內文以 `BREAKING CHANGE:` 段落明確說明影響與遷移方式。

## 文字、編碼與腳本

- 原始碼、Markdown、JSON、XML、PowerShell 腳本使用 UTF-8 無 BOM。
- VS/MSBuild 檔案例如 `.sln`、`.slnx`、`.csproj`、`.props`、`.targets` 要保留工具建立時既有的 UTF-8 BOM 決策；腳本更新這些檔案時不得強制移除 BOM。
- Windows/.NET/PowerShell 文字檔使用 CRLF；`.sh` 使用 LF。
- PowerShell 腳本檔首必須有 `#requires -Version 7.4`。
- 腳本必須設定 `$ErrorActionPreference = 'Stop'` 與 `Set-StrictMode -Version Latest`。
- 腳本說明、錯誤訊息與一般輸出使用正體中文台灣用語。
- 寫檔必須明確使用 UTF-8 無 BOM，不使用 `>` 或 `>>` 產生文字檔。

## Agent Skills

共用 skills 放在 `.agents/skills/`：

- `gameinput-version-tracking`
- `gameinput-binding-generation`
- `interop-abi-review`
- `package-release-validation`

CLI 專屬轉接應保持很薄。`CLAUDE.md` 只匯入本檔，不複製規則。
