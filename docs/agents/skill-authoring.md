# Skill 撰寫規範

本 repo 的 canonical skill 位置是 `.agents/skills/<kebab-case-name>/SKILL.md`。這個位置可被 Copilot CLI 與 Antigravity CLI 作為 project skill 使用，也能維持 Agent Skills 標準的可攜結構。Claude Code 的 project skill 位置是 `.claude/skills/`，目前不建立副本；若未來需要 Claude Code slash-skill，必須先規劃同步策略。

每個 skill 必須包含 YAML frontmatter：

```markdown
---
name: gameinput-version-tracking
description: 說明何時應該使用此 skill。
---
```

撰寫規則：

- `name` 必須與資料夾名稱一致。
- `description` 必須明確描述觸發時機，優先使用「Use when...」語意，讓支援自動選用的 CLI 能判斷是否載入。
- `name` 與資料夾名稱使用小寫英文字母、數字與連字號，避免空白與底線。
- 指令內容使用正體中文台灣用語。
- 可呼叫腳本時，優先呼叫 `eng/` 內共用腳本，不在 skill 內複製邏輯。
- 大型參考資料、範例或腳本放在 skill 目錄下的 `references/`、`examples/`、`scripts/` 或既有 repo 文件，並在 `SKILL.md` 以相對連結說明何時讀取。
- `allowed-tools`、`disable-model-invocation`、`user-invocable` 等欄位在各 CLI 的支援程度不同；只有在目標 CLI 已確認需要時才加入。
- 不預先允許 shell 權限；讓使用者或 CLI 的安全機制決定是否核准。
- 不使用舊版 `.agent/skills`；若看到該目錄，應搬移到 `.agents/skills` 後再更新文件。
