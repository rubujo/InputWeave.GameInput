#requires -Version 7.4

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force

$repoRoot = Get-RepoRoot
$baselinePath = Join-Path $repoRoot 'eng\gameinput-baseline.json'
$baseline = Get-Content -LiteralPath $baselinePath -Raw -Encoding utf8 | ConvertFrom-Json
$packageRoot = Get-GameInputPackageRoot -Version $baseline.packageVersion

$headerPath = Join-Path $packageRoot $baseline.headerPath
$redistPath = Join-Path $packageRoot $baseline.redistPath
if ((Get-Sha256 -Path $headerPath) -ne $baseline.headerSha256)
{
    throw 'GameInput.h SHA256 與 baseline 不一致。'
}

if ((Get-Sha256 -Path $redistPath) -ne $baseline.redistSha256)
{
    throw 'GameInputRedist.msi SHA256 與 baseline 不一致。'
}

$verifyRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'inputweave-gameinput-verify'
if (Test-Path -LiteralPath $verifyRoot)
{
    Remove-Item -LiteralPath $verifyRoot -Recurse -Force
}

[System.IO.Directory]::CreateDirectory($verifyRoot) | Out-Null

$actualGeneratedDir = Join-Path $repoRoot $baseline.generatedInteropOutputDir
$expectedGeneratedDir = Join-Path $verifyRoot 'Generated'
[System.IO.Directory]::CreateDirectory($expectedGeneratedDir) | Out-Null

$expectedGenerated = Join-Path $expectedGeneratedDir 'GameInputEnums.g.cs'
$expectedManifest = Join-Path $expectedGeneratedDir 'gameinput-abi-manifest.json'
$docsPath = Join-Path $repoRoot 'eng\gameinput-xml-docs.zh-TW.json'
& dotnet run --project (Join-Path $repoRoot 'tools\InputWeave.GameInput.BindingsGenerator\InputWeave.GameInput.BindingsGenerator.csproj') -- --header $headerPath --output $expectedGenerated --manifest $expectedManifest --interop-output-dir $expectedGeneratedDir --docs $docsPath
if ($LASTEXITCODE -ne 0)
{
    throw 'GameInput 繫結產生器執行失敗。'
}

$expectedGeneratedFiles = @(
    'GameInputEnums.g.cs',
    'gameinput-abi-manifest.json',
    'GameInputConstants.g.cs',
    'GameInputHResult.g.cs',
    'GameInputIids.g.cs',
    'GameInputCallbacks.NetFramework.g.cs',
    'GameInputStructs.g.cs',
    'GameInputNativeInterfaces.NetFramework.g.cs',
    'GameInputNativeInterfaces.Net10.g.cs'
)

$actualGeneratedFiles = @(Get-ChildItem -LiteralPath $actualGeneratedDir -File | Where-Object { $_.Name.EndsWith('.g.cs', [System.StringComparison]::Ordinal) -or $_.Name -eq 'gameinput-abi-manifest.json' } | Select-Object -ExpandProperty Name | Sort-Object)
$expectedGeneratedFileNames = @($expectedGeneratedFiles | Sort-Object)
if (@(Compare-Object -ReferenceObject $expectedGeneratedFileNames -DifferenceObject $actualGeneratedFiles).Count -ne 0)
{
    throw 'Interop/Generated 內的 GameInput 產生檔集合與 generator 預期不一致。'
}

foreach ($fileName in $expectedGeneratedFiles)
{
    $actualPath = Join-Path $actualGeneratedDir $fileName
    $expectedPath = Join-Path $expectedGeneratedDir $fileName

    if (-not (Test-Path -LiteralPath $actualPath -PathType Leaf))
    {
        throw "Interop/Generated 缺少 $fileName。"
    }

    $actual = Get-Content -LiteralPath $actualPath -Raw -Encoding utf8
    $expected = Get-Content -LiteralPath $expectedPath -Raw -Encoding utf8
    if ($actual -ne $expected)
    {
        throw "GameInput 產生檔 $fileName 不是由目前 baseline 產生。"
    }
}

$actualManifest = Join-Path $actualGeneratedDir 'gameinput-abi-manifest.json'
$actualAbi = Get-Content -LiteralPath $actualManifest -Raw -Encoding utf8
$manifest = $actualAbi | ConvertFrom-Json
foreach ($required in @('IGameInput', 'IGameInputReading', 'IGameInputDevice', 'IGameInputRawDeviceReport', 'IGameInputDispatcher', 'IGameInputForceFeedbackEffect', 'IGameInputMapper'))
{
    if (-not @($manifest.interfaces | Where-Object { $_.name -eq $required }))
    {
        throw "ABI manifest 缺少 $required。"
    }
}

if (-not @($manifest.hResults | Where-Object { $_.name -eq 'GAMEINPUT_E_INPUT_KIND_NOT_PRESENT' }))
{
    throw 'ABI manifest 缺少 GAMEINPUT_E_INPUT_KIND_NOT_PRESENT。'
}

$vtableSourcePath = Join-Path $actualGeneratedDir 'GameInputNativeInterfaces.Net10.g.cs'
$vtableSource = Get-Content -LiteralPath $vtableSourcePath -Raw -Encoding utf8
foreach ($interfaceDefinition in $manifest.interfaces)
{
    $vtableStructPattern = "internal unsafe struct $($interfaceDefinition.name)Vtbl\s*\{(?<body>.*?)\}"
    $vtableMatch = [System.Text.RegularExpressions.Regex]::Match($vtableSource, $vtableStructPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $vtableMatch.Success)
    {
        throw "GameInputNativeInterfaces.Net10.g.cs 缺少 $($interfaceDefinition.name)Vtbl 結構。"
    }

    $methodNamePattern = 'delegate\*\s+unmanaged\[Stdcall\]<[^>]*>\s+(?<name>\w+);'
    $vtableMethodNames = @([System.Text.RegularExpressions.Regex]::Matches($vtableMatch.Groups['body'].Value, $methodNamePattern) |
        ForEach-Object { $_.Groups['name'].Value } |
        Where-Object { $_ -notin @('QueryInterface', 'AddRef', 'Release') })
    $expectedMethodNames = @($interfaceDefinition.methods | ForEach-Object { $_.name })

    if (@(Compare-Object -ReferenceObject $expectedMethodNames -DifferenceObject $vtableMethodNames -SyncWindow 0).Count -ne 0)
    {
        throw "$($interfaceDefinition.name)Vtbl 的方法順序與 ABI manifest 不一致，vtable slot 順序錯誤會導致執行期呼叫錯誤的原生方法。"
    }
}

Write-Information 'GameInput baseline、redist 雜湊、低階 interop 產生檔、ABI manifest 與 net10 vtable 順序驗證通過。' -InformationAction Continue
