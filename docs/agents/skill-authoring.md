# Skill 撰寫規範

本 repo 的 canonical skill 位置是 `.agents/skills/<kebab-case-name>/SKILL.md`。這是 Codex CLI、GitHub Copilot CLI 與 Antigravity CLI 的共同可用位置；Claude Code 的 project skill 位置不同，因此本 repo 不預設建立 Claude Code skill 副本。

每個 skill 必須包含 YAML frontmatter：

```markdown
---
name: gameinput-version-update
description: Use when updating Microsoft.GameInput version, generated bindings, baseline hashes, or coverage docs.
---
```

撰寫規則：

- `name` 必須與資料夾名稱一致。
- `description` 必須明確描述觸發時機，優先使用 `Use when...` 語意，讓支援自動選用的 CLI 能判斷是否載入。
- `name` 與資料夾名稱使用小寫英文字母、數字與連字號，避免空白與底線。
- Frontmatter 只使用共同欄位：`name` 與 `description`。
- 指令內容使用正體中文台灣用語。
- 可呼叫腳本時，優先呼叫 `eng/` 內共用腳本，不在 skill 內複製邏輯。
- 大型參考資料、範例或腳本放在 skill 目錄下的 `references/`、`examples/`、`scripts/` 或既有 repo 文件，並在 `SKILL.md` 以相對連結說明何時讀取。
- 不使用工具專屬欄位，例如 `allowed-tools`、`disable-model-invocation`、`user-invocable`、`paths` 或動態 shell injection。
- 不預先允許 shell 權限；讓使用者或 CLI 的安全機制決定是否核准。
- 不使用 `.github/skills`、`.claude/skills` 或舊版 `.agent/skills`；若真的需要，必須先規劃同步與驗證方式。
