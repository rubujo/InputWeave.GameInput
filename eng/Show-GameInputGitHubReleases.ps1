#requires -Version 7.4

[CmdletBinding()]
param(
    [ValidateRange(1, 30)]
    [int]$Count = 5,

    [switch]$MarkReviewed,

    [switch]$FailOnUnreviewed
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force

$releases = Get-LatestGameInputGitHubRelease -Count $Count
if ($releases.Count -eq 0)
{
    throw '找不到 microsoftconnect/GameInput 的 GitHub Release。'
}

$latestTag = [string]$releases[0].tag_name
$tracking = Get-GameInputGitHubReleaseTracking
$lastReviewedTag = if ($null -ne $tracking) { [string]$tracking.lastReviewedTag } else { $null }

Write-Information "microsoftconnect/GameInput 最新 GitHub Release：$latestTag" -InformationAction Continue
Write-Information "上次核對到的 GitHub Release：$(if ($lastReviewedTag) { $lastReviewedTag } else { '（尚未核對過）' })" -InformationAction Continue
Write-Information '注意：GitHub Release tag 版號與 NuGet 套件版號採不同編號機制，不能直接比對版本字串；請人工核對下列 Release Notes 內容是否已反映在 eng/gameinput-version-notes.json 與相關文件。' -InformationAction Continue
Write-Information '' -InformationAction Continue

foreach ($release in $releases)
{
    $tag = [string]$release.tag_name
    $publishedAt = [string]$release.published_at
    $body = if ($null -ne $release.body) { [string]$release.body } else { '' }

    Write-Information "## $tag（發布於 $publishedAt）" -InformationAction Continue
    Write-Information $body -InformationAction Continue
    Write-Information '' -InformationAction Continue
}

if ($MarkReviewed)
{
    $trackingPath = Get-GameInputGitHubReleaseTrackingPath
    $reviewedAt = (Get-Date).ToString('yyyy-MM-dd')
    $note = 'GitHub Release tag 版號（例如 v3.3.195.0）與 NuGet 套件版號（例如 3.5.270）採不同編號機制，不能直接比對版本字串；每次追版時應人工核對 Release Notes 內容是否已反映在 eng/gameinput-version-notes.json，確認後執行 Show-GameInputGitHubReleases.ps1 -MarkReviewed 更新這份紀錄。'

    $tracking = [ordered]@{
        repository     = 'microsoftconnect/GameInput'
        lastReviewedTag = $latestTag
        lastReviewedAt  = $reviewedAt
        note            = $note
    }

    $json = $tracking | ConvertTo-Json -Depth 5
    Write-Utf8NoBomFile -Path $trackingPath -Content $json -NewLine CRLF

    Write-Information "已將 GitHub Release 核對紀錄更新為 $latestTag（$reviewedAt）。" -InformationAction Continue
    exit 0
}

if ($lastReviewedTag -ne $latestTag)
{
    $message = "microsoftconnect/GameInput 有尚未核對的新 Release：$latestTag（上次核對：$(if ($lastReviewedTag) { $lastReviewedTag } else { '無' })）。請人工核對上方 Release Notes，確認 eng/gameinput-version-notes.json 等文件是否需要更新，再執行本腳本加上 -MarkReviewed 標記已核對。"
    if ($FailOnUnreviewed)
    {
        throw $message
    }

    Write-Warning $message
    exit 1
}

Write-Information 'GitHub Release 已核對至最新版本。' -InformationAction Continue
