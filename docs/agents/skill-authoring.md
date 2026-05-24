# Skill 撰寫規範

每個 skill 必須位於 `.agents/skills/<kebab-case-name>/SKILL.md`，並包含 YAML frontmatter：

```markdown
---
name: gameinput-version-tracking
description: 說明何時應該使用此 skill。
---
```

撰寫規則：

- `name` 必須與資料夾名稱一致。
- `description` 必須明確描述觸發時機。
- 指令內容使用正體中文台灣用語。
- 可呼叫腳本時，優先呼叫 `eng/` 內共用腳本，不在 skill 內複製邏輯。
- 不預先允許 shell 權限；讓使用者或 CLI 的安全機制決定是否核准。
