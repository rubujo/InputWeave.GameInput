#requires -Version 7.4

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force

$repoRoot = Get-RepoRoot
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true)
$extensions = @('.cs', '.csproj', '.props', '.targets', '.sln', '.slnx', '.vcxproj', '.filters', '.xml', '.json', '.md', '.ps1', '.psm1', '.psd1', '.sh')
$excludedDirectories = @('.git', '.tmp', '.vs', 'bin', 'obj', 'packages', 'TestResults', 'artifacts')
$utf8BomAllowedExtensions = @('.sln', '.slnx', '.csproj', '.props', '.targets', '.vcxproj', '.filters')
$failures = [System.Collections.Generic.List[string]]::new()

function Test-IsExcludedPath
{
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $relative = [System.IO.Path]::GetRelativePath($repoRoot, $Path)
    $segments = $relative -split '[\\/]'
    foreach ($segment in $segments)
    {
        if ($excludedDirectories -contains $segment)
        {
            return $true
        }
    }

    return $false
}

$files = Get-ChildItem -LiteralPath $repoRoot -Recurse -File | Where-Object {
    $extensions -contains $_.Extension -and -not (Test-IsExcludedPath -Path $_.FullName)
}

foreach ($file in $files)
{
    $relativePath = [System.IO.Path]::GetRelativePath($repoRoot, $file.FullName)
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)

    $hasUtf8Bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    if ($hasUtf8Bom -and -not ($utf8BomAllowedExtensions -contains $file.Extension))
    {
        $failures.Add("$relativePath 使用 UTF-8 BOM。")
    }

    if ($bytes.Length -ge 2 -and (($bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) -or ($bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF)))
    {
        $failures.Add("$relativePath 使用 UTF-16 BOM。")
    }

    try
    {
        $text = $utf8Strict.GetString($bytes)
    }
    catch
    {
        $failures.Add("$relativePath 不是有效 UTF-8。")
        continue
    }

    if ($text.Contains("`0", [System.StringComparison]::Ordinal))
    {
        $failures.Add("$relativePath 可能是 UTF-16 或二進位內容。")
    }

    $expectedNewLine = if ($file.Extension -eq '.sh') { 'LF' } else { 'CRLF' }
    if ($expectedNewLine -eq 'CRLF')
    {
        if ([regex]::IsMatch($text, "(?<!`r)`n") -or [regex]::IsMatch($text, "`r(?!`n)"))
        {
            $failures.Add("$relativePath 換行不是純 CRLF。")
        }
    }
    else
    {
        if ($text.Contains("`r", [System.StringComparison]::Ordinal))
        {
            $failures.Add("$relativePath 換行不是純 LF。")
        }
    }

    if ($file.Extension -ne '.md')
    {
        $lines = $text -split "`r?`n"
        for ($index = 0; $index -lt $lines.Count; $index++)
        {
            if ($lines[$index] -match '\s+$')
            {
                $lineNumber = $index + 1
                $failures.Add("$relativePath 第 $lineNumber 行有尾端空白。")
                break
            }
        }
    }
}

$scriptAnalyzer = Get-Command Invoke-ScriptAnalyzer -ErrorAction SilentlyContinue
if ($null -ne $scriptAnalyzer)
{
    $settingsPath = Join-Path $repoRoot 'eng\PSScriptAnalyzerSettings.psd1'
    $results = Invoke-ScriptAnalyzer -Path $repoRoot -Recurse -Settings $settingsPath -Severity Warning,Error
    foreach ($result in $results)
    {
        $failures.Add("$($result.ScriptName):$($result.Line) $($result.RuleName) $($result.Message)")
    }
}
else
{
    Write-Warning '找不到 PSScriptAnalyzer；已略過 PowerShell 靜態分析。'
}

if ($failures.Count -gt 0)
{
    foreach ($failure in $failures)
    {
        Write-Error $failure -ErrorAction Continue
    }

    throw "文字、編碼或腳本品質檢查失敗，共 $($failures.Count) 項。"
}

Write-Information '文字、編碼、換行與腳本品質檢查通過。' -InformationAction Continue
