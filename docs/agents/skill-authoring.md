# 技能撰寫規範

本儲存庫的主要技能位置是 `.agents/skills/<kebab-case-name>/SKILL.md`。這是 Codex CLI、GitHub Copilot CLI 與 Antigravity CLI 的共同可用位置；Claude Code 的專案技能位置不同，因此本儲存庫不預設建立 Claude Code 技能副本。

每個技能必須包含 YAML 前置中繼資料：

```markdown
---
name: gameinput-version-update
description: 當需要更新 Microsoft.GameInput 版本、產生式繫結、基準雜湊或覆蓋率文件時使用。
---
```

撰寫規則：

- `name` 必須與資料夾名稱一致。
- `description` 必須明確描述觸發時機，讓支援自動選用的 CLI 能判斷是否載入。
- `name` 與資料夾名稱使用小寫英文字母、數字與連字號，避免空白與底線。
- YAML 前置中繼資料只使用共同欄位：`name` 與 `description`。
- 指令內容使用正體中文（台灣）用語。
- 可呼叫腳本時，優先呼叫 `eng/` 內共用腳本，不在技能內複製邏輯。
- 大型參考資料、範例或腳本放在技能目錄下的 `references/`、`examples/`、`scripts/` 或既有儲存庫文件，並在 `SKILL.md` 以相對連結說明何時讀取。
- 不使用工具專屬欄位，例如 `allowed-tools`、`disable-model-invocation`、`user-invocable`、`paths` 或動態 Shell 注入。
- 不預先允許 Shell 權限；讓使用者或 CLI 的安全機制決定是否核准。
- 不使用 `.github/skills`、`.claude/skills` 或舊版 `.agent/skills`；若真的需要，必須先規劃同步與驗證方式。
