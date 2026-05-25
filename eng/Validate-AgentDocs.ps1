#requires -Version 7.4

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force

$repoRoot = Get-RepoRoot
$failures = [System.Collections.Generic.List[string]]::new()
$agentsPath = Join-Path $repoRoot 'AGENTS.md'
$claudePath = Join-Path $repoRoot 'CLAUDE.md'
$skillsRoot = Join-Path $repoRoot '.agents\skills'
$legacyAgentPath = Join-Path $repoRoot '.agent'
$claudeSkillsRoot = Join-Path $repoRoot '.claude\skills'

if (-not (Test-Path -LiteralPath $agentsPath -PathType Leaf))
{
    $failures.Add('缺少 AGENTS.md。')
}
else
{
    $agentsBytes = [System.IO.File]::ReadAllBytes($agentsPath)
    if ($agentsBytes.Length -gt 16384)
    {
        $failures.Add('AGENTS.md 超過 16 KiB。')
    }
}

if (-not (Test-Path -LiteralPath $claudePath -PathType Leaf))
{
    $failures.Add('缺少 CLAUDE.md。')
}
else
{
    $claude = Get-Content -LiteralPath $claudePath -Raw -Encoding utf8
    if (-not $claude.StartsWith('@AGENTS.md', [System.StringComparison]::Ordinal))
    {
        $failures.Add('CLAUDE.md 必須以 @AGENTS.md 匯入開頭。')
    }

    if ($claude.Length -gt 256)
    {
        $failures.Add('CLAUDE.md 應維持薄轉接，請避免複製 AGENTS.md 規則。')
    }
}

if (Test-Path -LiteralPath $legacyAgentPath)
{
    $failures.Add('請使用 .agents/ 目錄，不要重新加入舊版 .agent/ 目錄。')
}

if (Test-Path -LiteralPath $claudeSkillsRoot)
{
    $failures.Add('目前不維護 .claude/skills 副本；若需要 Claude Code slash-skill，請先規劃同步策略。')
}

if (-not (Test-Path -LiteralPath $skillsRoot -PathType Container))
{
    $failures.Add('缺少 .agents/skills。')
}
else
{
    $skillDirectories = Get-ChildItem -LiteralPath $skillsRoot -Directory
    foreach ($skillDirectory in $skillDirectories)
    {
        if ($skillDirectory.Name -notmatch '^[a-z0-9]+(-[a-z0-9]+)*$')
        {
            $failures.Add("$($skillDirectory.Name) 不是 kebab-case skill 名稱。")
        }

        $skillPath = Join-Path $skillDirectory.FullName 'SKILL.md'
        if (-not (Test-Path -LiteralPath $skillPath -PathType Leaf))
        {
            $failures.Add("$($skillDirectory.Name) 缺少 SKILL.md。")
            continue
        }

        $skill = Get-Content -LiteralPath $skillPath -Raw -Encoding utf8
        if ($skill -notmatch '(?s)^---\s+name:\s*(?<name>[a-z0-9-]+)\s+description:\s*(?<description>.+?)\s+---')
        {
            $failures.Add("$($skillDirectory.Name) 的 SKILL.md 缺少有效 frontmatter。")
            continue
        }

        if ($Matches['name'] -ne $skillDirectory.Name)
        {
            $failures.Add("$($skillDirectory.Name) 的 frontmatter name 與資料夾名稱不一致。")
        }

        $description = [string]$Matches['description']
        if ([string]::IsNullOrWhiteSpace($description))
        {
            $failures.Add("$($skillDirectory.Name) 的 frontmatter description 不可為空。")
        }
        elseif ($description.Length -gt 1024)
        {
            $failures.Add("$($skillDirectory.Name) 的 frontmatter description 超過 1024 字元。")
        }
    }
}

if ($failures.Count -gt 0)
{
    foreach ($failure in $failures)
    {
        Write-Error $failure -ErrorAction Continue
    }

    throw "Agent 文件檢查失敗，共 $($failures.Count) 項。"
}

Write-Information 'Agent 文件與 Skill 結構檢查通過。' -InformationAction Continue
