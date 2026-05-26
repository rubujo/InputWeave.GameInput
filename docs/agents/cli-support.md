# 代理 CLI 支援策略

本儲存庫的代理規範採最大公因數：一份主規範、一個主要技能目錄，不為每個 CLI 建立內容副本。

## 維護規則

- `AGENTS.md` 是唯一主規範。Codex CLI、GitHub Copilot CLI 與 Antigravity CLI 都能讀取此檔。
- `CLAUDE.md` 只保留 `@AGENTS.md`，因為 Claude Code 讀 `CLAUDE.md` 而不是直接讀 `AGENTS.md`。
- `.agents/skills/` 是主要專案技能目錄。它覆蓋 Codex CLI、GitHub Copilot CLI 與 Antigravity CLI；Claude Code 若需要斜線技能命令，必須另行規劃 `.claude/skills` 鏡像副本，本儲存庫預設不維護第二份。
- `AGENTS.md` 放穩定規則；`.agents/skills/` 放低頻率、多步驟、任務導向流程。
- 不建立 `GEMINI.md`、`.github/copilot-instructions.md`、`.github/skills`、`.claude/skills` 或 `.agent/`。這些都是工具專屬入口，只有在明確支援該工具情境並有同步策略時才新增。
- 所有代理文件都必須使用正體中文（台灣）用語、UTF-8 無 BOM、CRLF。

## 需要另開規劃的情境

- 支援 GitHub.com Copilot Chat、Copilot 程式碼審查或 Copilot 雲端代理：新增 `.github/copilot-instructions.md` 或 `.github/instructions/**/*.instructions.md`。
- 讓 Claude Code 直接以 `/skill-name` 執行專案技能：建立 `.claude/skills` 鏡像副本與一致性檢查。
- 需要 Antigravity 專屬路徑範圍規則：新增 `.agents/rules`，但不得把它當成主規範替代品。
- 要跨儲存庫發佈技能：改用各工具的外掛或技能發佈機制，而不是在本儲存庫增加工具專屬副本。

## 官方依據

- Codex CLI：[`AGENTS.md` 搜尋規則](https://developers.openai.com/codex/guides/agents-md)、[技能](https://developers.openai.com/codex/skills)
- Claude Code：[`CLAUDE.md` 記憶](https://code.claude.com/docs/en/memory)、[技能](https://code.claude.com/docs/en/skills)
- GitHub Copilot CLI：[自訂指示](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-custom-instructions)、[代理技能](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-skills)
- Antigravity CLI：[規則](https://antigravity.google/docs/rules-workflows)、[技能](https://antigravity.google/docs/skills)、[Gemini CLI 遷移](https://antigravity.google/docs/gcli-migration)
