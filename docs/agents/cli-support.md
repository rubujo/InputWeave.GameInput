# Agent CLI 支援策略

本 repo 採用「一份主要規則，必要時才加薄轉接」：

| 工具 | 規範入口 | Skill 入口 | 本 repo 決策 |
| --- | --- | --- | --- |
| Codex CLI | 根目錄 `AGENTS.md`，必要時可用巢狀 `AGENTS.override.md` 覆蓋 | `.agents/skills/<skill>/SKILL.md` | 以 `AGENTS.md` 作為主規範，不建立額外 Codex 專用規則檔 |
| Claude Code | 根目錄 `CLAUDE.md` 或 `.claude/CLAUDE.md` | `.claude/skills/<skill>/SKILL.md` | `CLAUDE.md` 只用 `@AGENTS.md` 匯入主規範；目前不建立 `.claude/skills` 副本 |
| GitHub Copilot CLI | 根目錄 `AGENTS.md`，也支援 `.github/copilot-instructions.md` | `.github/skills/`、`.agents/skills/` 或 `.claude/skills/` | CLI 使用 `AGENTS.md` 與 `.agents/skills`；不預設建立 Copilot 專用 mirror |
| Antigravity CLI | 工作區 `AGENTS.md` 與 `GEMINI.md` | `.agents/skills/<skill>/SKILL.md` | 使用 `AGENTS.md` 與 `.agents/skills`；不建立 `GEMINI.md` 或舊版 `.agent/` |

## 維護規則

- `AGENTS.md` 放跨工具、跨任務都需要的專案規範。
- 多步驟、低頻率、特定任務的流程放進 `.agents/skills/<name>/SKILL.md`，不要把大型程序堆進 `AGENTS.md`。
- CLI 專屬檔案只能做薄轉接或記錄該 CLI 的載入限制，不複製 `AGENTS.md` 全文。
- 若要讓 Claude Code 以 `/skill-name` 直接叫用 project skill，必須另行規劃 `.claude/skills` 同步策略，避免 `.agents/skills` 與 `.claude/skills` 內容分歧。
- 若要支援 GitHub.com Copilot Chat、Copilot code review 或 Copilot cloud agent，再新增 `.github/copilot-instructions.md` 或 `.github/instructions/**/*.instructions.md`；這不是 Copilot CLI 的必要條件。
- 不使用舊版 `.agent/` 目錄；Antigravity 目前文件已將 workspace rules 與 skills 預設位置改為 `.agents/`。
- 所有 Agent 文件都必須使用正體中文台灣用語、UTF-8 無 BOM、CRLF。

## 官方依據

- Codex CLI：[`AGENTS.md` discovery](https://developers.openai.com/codex/guides/agents-md)、[skills](https://developers.openai.com/codex/skills)
- Claude Code：[`CLAUDE.md` memory](https://code.claude.com/docs/en/memory)、[skills](https://code.claude.com/docs/en/skills)
- GitHub Copilot CLI：[custom instructions](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-custom-instructions)、[agent skills](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-skills)
- Antigravity CLI：[rules](https://antigravity.google/docs/rules-workflows)、[skills](https://antigravity.google/docs/skills)、[Gemini CLI migration](https://antigravity.google/docs/gcli-migration)
