# Agent CLI 支援策略

本 repo 的 Agent 規範採最大公因數：一份主規範、一個 canonical skill 目錄，不為每個 CLI 建立內容副本。

## 維護規則

- `AGENTS.md` 是唯一主規範。Codex CLI、GitHub Copilot CLI 與 Antigravity CLI 都能讀取此檔。
- `CLAUDE.md` 只保留 `@AGENTS.md`，因為 Claude Code 讀 `CLAUDE.md` 而不是直接讀 `AGENTS.md`。
- `.agents/skills/` 是 canonical project skill 位置。它覆蓋 Codex CLI、GitHub Copilot CLI 與 Antigravity CLI；Claude Code 若需要 slash-skill，必須另行規劃 `.claude/skills` mirror，不在本 repo 預設維護第二份。
- `AGENTS.md` 放穩定規則；`.agents/skills/` 放低頻率、多步驟、任務導向流程。
- 不建立 `GEMINI.md`、`.github/copilot-instructions.md`、`.github/skills`、`.claude/skills` 或 `.agent/`。這些都是工具專屬入口，只有在明確支援該工具情境並有同步策略時才新增。
- 所有 Agent 文件都必須使用正體中文台灣用語、UTF-8 無 BOM、CRLF。

## 需要另開規劃的情境

- 支援 GitHub.com Copilot Chat、Copilot code review 或 Copilot cloud agent：新增 `.github/copilot-instructions.md` 或 `.github/instructions/**/*.instructions.md`。
- 讓 Claude Code 直接以 `/skill-name` 執行 project skill：建立 `.claude/skills` mirror 與一致性檢查。
- 需要 Antigravity 專屬 path-scoped rules：新增 `.agents/rules`，但不得把它當成主規範替代品。
- 要跨 repo 發佈 skills：改用各工具的 plugin / skill 發佈機制，而不是在本 repo 增加工具專屬副本。

## 官方依據

- Codex CLI：[`AGENTS.md` discovery](https://developers.openai.com/codex/guides/agents-md)、[skills](https://developers.openai.com/codex/skills)
- Claude Code：[`CLAUDE.md` memory](https://code.claude.com/docs/en/memory)、[skills](https://code.claude.com/docs/en/skills)
- GitHub Copilot CLI：[custom instructions](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-custom-instructions)、[agent skills](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-skills)
- Antigravity CLI：[rules](https://antigravity.google/docs/rules-workflows)、[skills](https://antigravity.google/docs/skills)、[Gemini CLI migration](https://antigravity.google/docs/gcli-migration)
