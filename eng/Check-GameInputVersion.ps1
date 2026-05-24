#requires -Version 7.4

[CmdletBinding()]
param(
    [switch]$FailOnOutdated
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force

$currentVersion = Get-GameInputPackageVersion
$latestVersion = Get-LatestGameInputVersion

Write-Information "目前 Microsoft.GameInput 版本：$currentVersion" -InformationAction Continue
Write-Information "NuGet 最新 Microsoft.GameInput 版本：$latestVersion" -InformationAction Continue

if ($currentVersion -ne $latestVersion)
{
    $message = "Microsoft.GameInput 已有新版 $latestVersion，目前基準仍是 $currentVersion。"
    if ($FailOnOutdated)
    {
        throw $message
    }

    Write-Warning $message
    exit 1
}

Write-Information 'Microsoft.GameInput 已是最新基準版本。' -InformationAction Continue
