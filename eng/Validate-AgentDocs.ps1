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
$disallowedPaths = @(
    'GEMINI.md',
    '.agent',
    '.github\copilot-instructions.md',
    '.github\instructions',
    '.github\skills',
    '.claude\skills'
)
$expectedSkillNames = @(
    'gameinput-binding-generation',
    'gameinput-version-update',
    'package-release-validation'
)
$allowedSkillFrontmatterKeys = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('name', 'description'),
    [System.StringComparer]::Ordinal
)

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
    if ($claude.Trim() -ne '@AGENTS.md')
    {
        $failures.Add('CLAUDE.md 必須只保留 @AGENTS.md 匯入，不複製規則。')
    }
}

foreach ($relativePath in $disallowedPaths)
{
    $path = Join-Path $repoRoot $relativePath
    if (Test-Path -LiteralPath $path)
    {
        $failures.Add("不得重新加入 $relativePath；請維持 AGENTS.md 與 .agents/skills 的最小 Agent 結構。")
    }
}

if (-not (Test-Path -LiteralPath $skillsRoot -PathType Container))
{
    $failures.Add('缺少 .agents/skills。')
}
else
{
    $skillDirectories = @(Get-ChildItem -LiteralPath $skillsRoot -Directory | Sort-Object Name)
    $actualSkillNames = [string[]]($skillDirectories | Select-Object -ExpandProperty Name)

    foreach ($expectedSkillName in $expectedSkillNames)
    {
        if ($actualSkillNames -notcontains $expectedSkillName)
        {
            $failures.Add("缺少必要 skill：$expectedSkillName。")
        }
    }

    foreach ($actualSkillName in $actualSkillNames)
    {
        if ($expectedSkillNames -notcontains $actualSkillName)
        {
            $failures.Add("未規劃的 skill：$actualSkillName。新增 skill 前請先確認是否為跨工具共同流程。")
        }
    }

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
        $frontmatterMatch = [System.Text.RegularExpressions.Regex]::Match(
            $skill,
            '\A---\r?\n(?<frontmatter>.*?)\r?\n---',
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )
        if (-not $frontmatterMatch.Success)
        {
            $failures.Add("$($skillDirectory.Name) 的 SKILL.md 缺少有效 frontmatter。")
            continue
        }

        $frontmatter = $frontmatterMatch.Groups['frontmatter'].Value
        $metadata = @{}
        foreach ($line in $frontmatter -split '\r?\n')
        {
            if ([string]::IsNullOrWhiteSpace($line))
            {
                continue
            }

            $keyMatch = [System.Text.RegularExpressions.Regex]::Match($line, '^(?<key>[A-Za-z0-9_-]+)\s*:\s*(?<value>.*)$')
            if (-not $keyMatch.Success)
            {
                $failures.Add("$($skillDirectory.Name) 的 frontmatter 行無法解析：$line")
                continue
            }

            $key = $keyMatch.Groups['key'].Value
            if (-not $allowedSkillFrontmatterKeys.Contains($key))
            {
                $failures.Add("$($skillDirectory.Name) 使用工具專屬 frontmatter 欄位：$key。只允許 name 與 description。")
                continue
            }

            $metadata[$key] = $keyMatch.Groups['value'].Value.Trim()
        }

        if (-not $metadata.ContainsKey('name'))
        {
            $failures.Add("$($skillDirectory.Name) 的 frontmatter 缺少 name。")
        }
        elseif ($metadata['name'] -ne $skillDirectory.Name)
        {
            $failures.Add("$($skillDirectory.Name) 的 frontmatter name 與資料夾名稱不一致。")
        }
        elseif ($metadata['name'].Length -gt 64)
        {
            $failures.Add("$($skillDirectory.Name) 的 frontmatter name 超過 64 字元。")
        }

        if (-not $metadata.ContainsKey('description'))
        {
            $failures.Add("$($skillDirectory.Name) 的 frontmatter 缺少 description。")
        }
        else
        {
            $description = [string]$metadata['description']
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
