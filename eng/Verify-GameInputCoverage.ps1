#requires -Version 7.4

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force

$repoRoot = Get-RepoRoot
$manifestPath = Join-Path $repoRoot 'src\InputWeave.GameInput\Interop\Generated\gameinput-abi-manifest.json'
$reportPath = Join-Path $repoRoot 'docs\gameinput-api-coverage.md'
$baselinePath = Join-Path $repoRoot 'eng\gameinput-baseline.json'
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

if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf))
{
    throw '找不到 GameInput 版本基準。'
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding utf8 | ConvertFrom-Json
$baseline = Get-Content -LiteralPath $baselinePath -Raw -Encoding utf8 | ConvertFrom-Json
$wrapperCoverage = Get-Content -LiteralPath (Join-Path $repoRoot 'eng\gameinput-wrapper-coverage.json') -Raw -Encoding utf8 | ConvertFrom-Json
if ($manifest.apiVersion -ne $baseline.apiVersion -or $wrapperCoverage.apiVersion -ne $baseline.apiVersion)
{
    Add-Failure -Message '基準、ABI manifest 與逐方法包裝清單的 API 版本不一致。'
}

foreach ($interface in $manifest.interfaces)
{
    $entry = $wrapperCoverage.interfaces.PSObject.Properties[$interface.name]
    if ($null -eq $entry)
    {
        Add-Failure -Message "逐方法包裝清單缺少 $($interface.name)。"
        continue
    }

    $wrapper = $entry.Value
    $wrapperSource = (Get-ChildItem -LiteralPath $sourceRoot -File -Filter "$($wrapper.wrapper)*.cs" | ForEach-Object {
        Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
    }) -join "`n"
    $expectedMethods = @($interface.methods | ForEach-Object { $_.name } | Sort-Object)
    if (@(Compare-Object $expectedMethods @($wrapper.methods | Sort-Object)).Count -ne 0)
    {
        Add-Failure -Message "$($interface.name) 的逐方法清單與主標頭不一致。"
    }

    foreach ($method in $interface.methods)
    {
        $callPattern = '\.' + [regex]::Escape($method.name) + '(?:\s*\(|\s*[,\)])'
        if (-not [regex]::IsMatch($wrapperSource, $callPattern))
        {
            Add-Failure -Message "$($wrapper.wrapper) 缺少 $($interface.name).$($method.name) 的原生呼叫。"
        }
    }
}

foreach ($category in @('enums', 'structs', 'callbacks', 'interfaces', 'hResults', 'constants', 'functions'))
{
    $labels = @{ enums = '列舉'; structs = '結構'; callbacks = '回呼委派'; interfaces = 'COM Interface'; hResults = 'HRESULT'; constants = '常數'; functions = '原生函式' }
    $count = @($manifest.$category).Count
    Assert-TextContent -Text (Get-Content -LiteralPath $reportPath -Raw -Encoding utf8) -Expected "$($labels[$category])：$count / $count" -FailureMessage "覆蓋率文件的 $category 數量與 ABI manifest 不符。"
}
$methodCount = @($manifest.interfaces | ForEach-Object { $_.methods }).Count
Assert-TextContent -Text (Get-Content -LiteralPath $reportPath -Raw -Encoding utf8) -Expected "COM 方法：$methodCount / $methodCount" -FailureMessage '覆蓋率文件的 COM 方法數量與 ABI manifest 不符。'

foreach ($function in $manifest.functions)
{
    $mapping = $wrapperCoverage.functions.PSObject.Properties[$function.name]
    if ($null -eq $mapping)
    {
        Add-Failure -Message "缺少原生函式 $($function.name) 的受控入口對照。"
        continue
    }
    $clientSource = Get-Content -LiteralPath (Join-Path $sourceRoot 'GameInputClient.cs') -Raw -Encoding utf8
    Assert-TextContent -Text $clientSource -Expected 'public static GameInputClient Create()' -FailureMessage '缺少 GameInput 建立入口。'
    Assert-TextContent -Text $clientSource -Expected '.GameInputInitialize(' -FailureMessage '建立入口未呼叫原生 GameInputInitialize。'
    if ($function.isInline -and $mapping.Value.equivalent -ne 'GameInputInitialize')
    {
        Add-Failure -Message "inline 函式 $($function.name) 的等價入口不正確。"
    }
}
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
    Assert-TextContent -Text $allSource -Expected "struct $($interface.name)(IntPtr pointer)" -FailureMessage "低階 interop 缺少 COM interface $($interface.name) 的 vtable 包裝結構。"
    Assert-TextContent -Text $allSource -Expected "struct $($interface.name)Vtbl" -FailureMessage "低階 interop 缺少 COM interface $($interface.name) 的 vtable 結構。"
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
Assert-TextContent -Text $report -Expected "Microsoft.GameInput $($baseline.packageVersion)" -FailureMessage 'coverage report 未標示 GameInput baseline。'

if ($failures.Count -gt 0)
{
    foreach ($failure in $failures)
    {
        Write-Error $failure -ErrorAction Continue
    }

    throw "GameInput API coverage 驗證失敗，共 $($failures.Count) 項。"
}

Write-Information 'GameInput API coverage 驗證通過：低階 interop、高階 wrapper、文件報告皆為 v0.0.1 100% coverage。' -InformationAction Continue
