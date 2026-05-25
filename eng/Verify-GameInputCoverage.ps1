#requires -Version 7.4

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force

$repoRoot = Get-RepoRoot
$manifestPath = Join-Path $repoRoot 'src\InputWeave.GameInput\Interop\Generated\gameinput-abi-manifest.json'
$reportPath = Join-Path $repoRoot 'docs\gameinput-api-coverage.md'
$sourceRoot = Join-Path $repoRoot 'src\InputWeave.GameInput'
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure
{
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Message
    )

    $failures.Add($Message)
}

function Assert-TextContent
{
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Text,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Expected,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$FailureMessage
    )

    if (-not $Text.Contains($Expected, [System.StringComparison]::Ordinal))
    {
        Add-Failure -Message $FailureMessage
    }
}

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf))
{
    throw '找不到 GameInput ABI manifest。'
}

if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf))
{
    throw '找不到 GameInput API coverage report。'
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding utf8 | ConvertFrom-Json
$allSource = (Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs' | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"

$report = Get-Content -LiteralPath $reportPath -Raw -Encoding utf8

foreach ($enum in $manifest.enums)
{
    Assert-TextContent -Text $allSource -Expected "enum $($enum.name)" -FailureMessage "低階 interop 缺少 enum $($enum.name)。"
}

foreach ($struct in $manifest.structs)
{
    Assert-TextContent -Text $allSource -Expected "struct $($struct.name)" -FailureMessage "低階 interop 缺少 struct $($struct.name)。"
}

foreach ($callback in $manifest.callbacks)
{
    Assert-TextContent -Text $allSource -Expected "delegate void $($callback.name)" -FailureMessage "低階 interop 缺少 callback $($callback.name)。"
}

foreach ($interface in $manifest.interfaces)
{
    Assert-TextContent -Text $allSource -Expected "interface $($interface.name)" -FailureMessage "低階 interop 缺少 COM interface $($interface.name)。"
}

foreach ($hresult in $manifest.hResults)
{
    Assert-TextContent -Text $allSource -Expected "0x$($hresult.value)" -FailureMessage "低階 interop 缺少 HRESULT $($hresult.name)。"
}

$requiredWrapperSurface = @(
    'GameInputClient',
    'GameInputDevice',
    'GameInputReading',
    'GameInputCallbackRegistration',
    'GameInputDispatcher',
    'GameInputMapper',
    'GameInputRawDeviceReport',
    'GameInputForceFeedbackEffect',
    'GameInputForceFeedback',
    'GameInputDeviceManager',
    'GameInputDeviceInfoSnapshot',
    'GameInputHapticInfoSnapshot',
    'GetDeviceInfoSnapshot',
    'GetHapticInfoSnapshot',
    'OpenSafeWaitHandle',
    'CopyRawData(byte[] buffer, int offset, int count)',
    'SetRawData(byte[] data, int offset, int count)'
)

foreach ($surface in $requiredWrapperSurface)
{
    Assert-TextContent -Text $allSource -Expected $surface -FailureMessage "高階 wrapper coverage 缺少 $surface。"
}

Assert-TextContent -Text $report -Expected '缺口：0' -FailureMessage 'coverage report 未標示缺口為 0。'
Assert-TextContent -Text $report -Expected 'InputWeave.GameInput v0.0.1' -FailureMessage 'coverage report 未標示 v0.0.1。'
Assert-TextContent -Text $report -Expected 'Microsoft.GameInput 3.4.218' -FailureMessage 'coverage report 未標示 GameInput baseline。'

if ($failures.Count -gt 0)
{
    foreach ($failure in $failures)
    {
        Write-Error $failure -ErrorAction Continue
    }

    throw "GameInput API coverage 驗證失敗，共 $($failures.Count) 項。"
}

Write-Information 'GameInput API coverage 驗證通過：低階 interop、高階 wrapper、文件報告皆為 v0.0.1 100% coverage。' -InformationAction Continue
