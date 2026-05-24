# Agent CLI 支援策略

本 repo 採用「一份主要規則，多個薄轉接」：

- `AGENTS.md` 是 Codex CLI、Copilot CLI、Antigravity CLI 的主要共用指引。
- `.agents/skills/` 是共用 Agent Skills 目錄。
- `CLAUDE.md` 只匯入 `@AGENTS.md`，避免 Claude Code 規則重複。
- 不預設建立 `.github/copilot-instructions.md`；Copilot CLI 已可讀取根目錄 `AGENTS.md`。
- 不預設建立 Antigravity plugin；只有需要跨 repo 發佈技能時才新增 plugin 包裝。

所有 Agent 文件都必須使用正體中文台灣用語、UTF-8 無 BOM、CRLF。
