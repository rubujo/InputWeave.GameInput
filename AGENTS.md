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
- GameInput 覆蓋率檢查：`pwsh ./eng/Verify-GameInputCoverage.ps1`

## GameInput 維護邊界

- 目前基準是 `Microsoft.GameInput` `3.4.218`，API 版本 `3`。
- 修改互通層、版本基準或發佈包裝前，先使用對應 `.agents/skills/` 流程；不要把多步驟程序塞回本檔。
- `src/InputWeave.GameInput/Interop/Generated/` 下的 `.g.cs` 與 `gameinput-abi-manifest.json` 必須由產生器產生；不要手改產生式互通層。
- 產生式互通層的 XML 文件註解來源是 `eng/gameinput-xml-docs.json`；若缺文件，修改文件來源與產生器後重產。
- `docs/gameinput-api-coverage.md` 的 v0.0.1 覆蓋率必須維持缺口為 0，發佈前需跑 `eng/Verify-GameInputCoverage.ps1`。

## 程式碼規範

- C# 使用 Allman braces、4 空白縮排、每行一個敘述。
- C# 檔案一律使用 File-scoped Namespace；新增或重產 `.cs` 時不得回到 Block-scoped Namespace。
- C# 程式碼必須遵循 VS2026 / `dotnet format` 的 Code Style 與 Analyzer 建議，並配合 `LangVersion latest` 使用 C# 最新穩定語法。
- 若 VS2026 Lint 要求可安全套用的新語法，例如 Collection Expression `[]`、更精準的 Overload 或更具體的回傳型別，應更新程式碼而不是壓制規則。
- 不得新增 `#pragma warning disable` 來壓制 C# Analyzer 或 VS2026 Lint；若出現警告，應修正程式碼、產生器或 `.editorconfig` 規則來源。
- P/Invoke 在 `net10.0-windows` 等現代 TFM 必須使用 `LibraryImport` 來源產生器；`DllImport` 只可存在於 `NETFRAMEWORK` 專用相容檔。
- 本專案預設維持純受控包裝程式庫；GameInput 執行階段選擇與載入診斷由受控載入器實作，不導入原生 API 橋接 DLL，除非另有單檔發佈或原生診斷需求並先另行規劃。
- Public API 名稱維持英文技術命名；XML 文件註解採美式英文與正體中文（台灣）雙語並行，同一個標籤內先寫英文、再寫中文，兩種語言都必須傳達相同內容。
- 所有 public/protected type、member、enum member、delegate、方法參數與非 void 回傳值都必須有 XML 文件註解；不得忽略 `CS1591`。
- `InputWeave.GameInput.Interop` 保留原生 GameInput 識別字，方便對照 Microsoft `GameInput.h`。
- 一般使用者應優先使用受控包裝 API、快照、管理器、建構器與 Safe Handle API；低階互通層僅作為進階逃生出口。
- COM 物件與原生讀取資料必須有明確 `IDisposable` 生命週期。
- 不要把 `GameInputRedist.msi`、`GameInputRedist.dll` 或原生橋接 DLL 包進包裝套件。

## Git 提交規範

- Git Commit 必須使用約定式提交格式，例如 `feat: 建立 GameInput 包裝程式庫`。
- Commit 主旨與內文必須使用正體中文（台灣）用語；Type Token 依約定式提交維持英文小寫。
- 禁止一行提交；主旨後必須空一行，並撰寫至少一段內文說明變更內容與驗證重點。
- 主旨使用祈使或描述式短句，不加句號；內文可使用段落或條列。
- 若提交包含破壞性變更，必須在內文以 `BREAKING CHANGE:` 段落明確說明影響與遷移方式。

## 文字、編碼與腳本

- 原始碼、Markdown、JSON、XML、PowerShell 腳本使用 UTF-8 無 BOM。
- VS/MSBuild 檔案例如 `.sln`、`.slnx`、`.csproj`、`.props`、`.targets` 要保留工具建立時既有的 UTF-8 BOM 決策；腳本更新這些檔案時不得強制移除 BOM。
- Windows/.NET/PowerShell 文字檔使用 CRLF；`.sh` 使用 LF。
- PowerShell 腳本檔首必須有 `#requires -Version 7.4`。
- 腳本必須設定 `$ErrorActionPreference = 'Stop'` 與 `Set-StrictMode -Version Latest`。
- 腳本說明、錯誤訊息與一般輸出使用正體中文（台灣）用語。
- 寫檔必須明確使用 UTF-8 無 BOM，不使用 `>` 或 `>>` 產生文字檔。

## 代理規範與技能

`AGENTS.md` 是唯一主規範。它只放每次工作都需要的專案規則；低頻率、多步驟流程放進 `.agents/skills/<name>/SKILL.md` 或一般文件。

- `CLAUDE.md` 只保留 `@AGENTS.md`，讓 Claude Code 匯入同一份主規範。
- 不建立 `GEMINI.md`、`.github/copilot-instructions.md`、`.github/skills`、`.claude/skills` 或舊版 `.agent/`；新增這些入口前必須先確認目標工具與同步策略。
- `.agents/skills/` 是本儲存庫的主要技能目錄，採 Agent Skills 最大公因數：`SKILL.md`、`name`、`description` 與 Markdown 內容。
- 技能的 YAML 前置中繼資料不使用工具專屬欄位，例如 `allowed-tools`、`disable-model-invocation` 或 `user-invocable`。

目前保留的專案技能：

- `gameinput-version-update`
- `gameinput-binding-generation`
- `package-release-validation`
