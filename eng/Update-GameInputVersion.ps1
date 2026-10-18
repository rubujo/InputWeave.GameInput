#requires -Version 7.4

[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+(\.\d+){1,2}$')]
    [string]$Version,

    [ValidateSet(4)]
    [int]$ApiVersion
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force

$repoRoot = Get-RepoRoot
$baselinePath = Join-Path $repoRoot 'eng\gameinput-baseline.json'
if ([string]::IsNullOrWhiteSpace($Version))
{
    $Version = Get-LatestGameInputVersion
}

$parsedVersion = [System.Version]::Parse($Version)
$versionSeries = "$($parsedVersion.Major).$($parsedVersion.Minor)"
$versionNotesPath = Join-Path $repoRoot 'eng\gameinput-version-notes.json'
if (-not (Test-Path -LiteralPath $versionNotesPath -PathType Leaf))
{
    throw '找不到 eng/gameinput-version-notes.json。'
}

$versionNotesDocument = Get-Content -LiteralPath $versionNotesPath -Raw -Encoding utf8 | ConvertFrom-Json
$versionNotesProperty = $versionNotesDocument.PSObject.Properties[$versionSeries]
if ($null -eq $versionNotesProperty)
{
    throw "缺少 Microsoft.GameInput $versionSeries 系列的正體中文版本異動摘要。請先依 NuGet 套件 README 更新 eng/gameinput-version-notes.json。"
}

$versionNoteItems = @($versionNotesProperty.Value)
if ($versionNoteItems.Count -eq 0 -or @($versionNoteItems | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -ne $versionNoteItems.Count)
{
    throw "Microsoft.GameInput $versionSeries 系列的版本異動摘要不可為空。"
}

$formattedVersionNotes = ($versionNoteItems | ForEach-Object { "- $_" }) -join [Environment]::NewLine

$workRoot = Join-Path $repoRoot ".tmp\gameinput-update\$Version"
$nupkgPath = Join-Path $workRoot "Microsoft.GameInput.$Version.nupkg"
$extractPath = Join-Path $workRoot 'package'
[System.IO.Directory]::CreateDirectory($workRoot) | Out-Null

$packageUri = "https://www.nuget.org/api/v2/package/Microsoft.GameInput/$Version"
Write-Information "下載 Microsoft.GameInput $Version。" -InformationAction Continue
Invoke-WebRequest -Uri $packageUri -OutFile $nupkgPath

if (Test-Path -LiteralPath $extractPath)
{
    Remove-Item -LiteralPath $extractPath -Recurse -Force
}

Expand-Archive -LiteralPath $nupkgPath -DestinationPath $extractPath -Force

# 套件版本與包裝 API 版本分開維護；主標頭升版時選用官方相容標頭。
$currentBaseline = Get-Content -LiteralPath $baselinePath -Raw -Encoding utf8 | ConvertFrom-Json
$apiVersion = if ($PSBoundParameters.ContainsKey('ApiVersion')) { $ApiVersion } else { [int]$currentBaseline.apiVersion }
$headerRelativePath = 'native/include/GameInput.h'
$headerPath = Join-Path $extractPath $headerRelativePath
$rootHeaderText = Get-Content -LiteralPath $headerPath -Raw -Encoding utf8
$rootApiMatch = [regex]::Match($rootHeaderText, '(?m)^#define GAMEINPUT_API_VERSION (\d+)')
if (-not $rootApiMatch.Success)
{
    throw '無法辨識 GameInput 主標頭的 API 版本。'
}

if ([int]$rootApiMatch.Groups[1].Value -ne $apiVersion)
{
    $headerRelativePath = "native/include/v$apiVersion/GameInput.h"
    $headerPath = Join-Path $extractPath $headerRelativePath
}
$redistPath = Join-Path $extractPath 'redist\GameInputRedist.msi'
if (-not (Test-Path -LiteralPath $headerPath -PathType Leaf))
{
    throw "下載的 NuGet 套件缺少 $headerRelativePath。"
}

$selectedHeaderText = Get-Content -LiteralPath $headerPath -Raw -Encoding utf8
$selectedApiMatch = [regex]::Match($selectedHeaderText, '(?m)^#define GAMEINPUT_API_VERSION (\d+)')
if (-not $selectedApiMatch.Success -or [int]$selectedApiMatch.Groups[1].Value -ne $apiVersion)
{
    throw "選定標頭的 API 版本與目前包裝 API $apiVersion 不一致。"
}

if (-not (Test-Path -LiteralPath $redistPath -PathType Leaf))
{
    throw '下載的 NuGet 套件缺少 redist/GameInputRedist.msi。'
}

$nupkgHash = Get-Sha256 -Path $nupkgPath
$headerHash = Get-Sha256 -Path $headerPath
$redistHash = Get-Sha256 -Path $redistPath

$propsPath = Join-Path $repoRoot 'Directory.Packages.props'
$propsContent = Get-Content -LiteralPath $propsPath -Raw -Encoding utf8
$propsContent = [regex]::Replace(
    $propsContent,
    '(<PackageVersion Include="Microsoft\.GameInput" Version=")[^"]+(" />)',
    "`${1}$Version`${2}")
Write-Utf8PreservingBomFile -Path $propsPath -Content $propsContent

$generatedEnumsPath = Join-Path $repoRoot 'src\InputWeave.GameInput\Interop\Generated\GameInputEnums.g.cs'
$generatedAbiManifestPath = Join-Path $repoRoot 'src\InputWeave.GameInput\Interop\Generated\gameinput-abi-manifest.json'
$generatedInteropOutputDir = Join-Path $repoRoot 'src\InputWeave.GameInput\Interop\Generated'
$docsPath = Join-Path $repoRoot 'eng\gameinput-xml-docs.json'
& dotnet run --project (Join-Path $repoRoot 'tools\InputWeave.GameInput.BindingsGenerator\InputWeave.GameInput.BindingsGenerator.csproj') -- --header $headerPath --output $generatedEnumsPath --manifest $generatedAbiManifestPath --interop-output-dir $generatedInteropOutputDir --docs $docsPath
if ($LASTEXITCODE -ne 0)
{
    throw 'GameInput 繫結產生器執行失敗。'
}

$baseline = @"
{
  "packageId": "Microsoft.GameInput",
  "packageVersion": "$Version",
  "apiVersion": $apiVersion,
  "nupkgSha256": "$nupkgHash",
  "headerPath": "$headerRelativePath",
  "headerSha256": "$headerHash",
  "redistPath": "redist/GameInputRedist.msi",
  "redistSha256": "$redistHash",
  "generatedEnumsPath": "src/InputWeave.GameInput/Interop/Generated/GameInputEnums.g.cs",
  "generatedAbiManifestPath": "src/InputWeave.GameInput/Interop/Generated/gameinput-abi-manifest.json",
  "generatedInteropOutputDir": "src/InputWeave.GameInput/Interop/Generated",
  "source": "https://www.nuget.org/packages/Microsoft.GameInput/$Version"
}
"@
Write-Utf8NoBomFile -Path $baselinePath -Content ($baseline + [Environment]::NewLine)

$versionReferencePaths = @(
    'AGENTS.md',
    'README.md',
    'docs\gameinput-api-coverage.md',
    'samples\InputWeave.GameInput.Samples\Program.cs',
    'tests\InputWeave.GameInput.Tests\GameInputCoverageTests.cs'
)

foreach ($relativePath in $versionReferencePaths)
{
    $referencePath = Join-Path $repoRoot $relativePath
    $referenceContent = Get-Content -LiteralPath $referencePath -Raw -Encoding utf8
    $updatedReferenceContent = [regex]::Replace(
        $referenceContent,
        '(Microsoft\.GameInput`?\s+`?)\d+\.\d+\.\d+(`?)',
        {
            param($match)

            return "$($match.Groups[1].Value)$Version$($match.Groups[2].Value)"
        })
    $updatedReferenceContent = [regex]::Replace(
        $updatedReferenceContent,
        'https://www\.nuget\.org/packages/Microsoft\.GameInput/\d+\.\d+\.\d+',
        "https://www.nuget.org/packages/Microsoft.GameInput/$Version")

    if ($relativePath -eq 'docs\gameinput-api-coverage.md')
    {
        $updatedReferenceContent = $updatedReferenceContent.Replace([string]$currentBaseline.headerPath, $headerRelativePath)
        $updatedReferenceContent = [regex]::Replace(
            $updatedReferenceContent,
            '(最後核對日期：)\d{4}-\d{2}-\d{2}',
            "`${1}$(Get-Date -Format 'yyyy-MM-dd')")
    }

    Write-Utf8NoBomFile -Path $referencePath -Content $updatedReferenceContent
}

$report = @"
# GameInput 版本報告

目前基準版本：``Microsoft.GameInput`` ``$Version``

目前包裝程式庫版本：``InputWeave.GameInput v0.0.1``，NuGet / MSBuild 版本為 ``0.0.1``。

- 包裝 API 版本：``$apiVersion``（套件主標頭 API 版本：``$($rootApiMatch.Groups[1].Value)``）
- NuGet 套件 SHA256：``$nupkgHash``
- ``$headerRelativePath`` SHA256：``$headerHash``
- ``redist/GameInputRedist.msi`` SHA256：``$redistHash``

低階互通層來源：``src/InputWeave.GameInput/Interop/Generated/`` 下的列舉、常數、HRESULT、IID、回呼委派、結構配置、COM 介面與 ``gameinput-abi-manifest.json`` 均由目前基準的 ``$headerRelativePath`` 產生。套件追版預設維持目前包裝 API 範圍；明確遷移時使用 ``-ApiVersion`` 指定版本，並完成產生器、受控包裝與逐方法覆蓋率驗證。

## Microsoft 官方 $versionSeries 版本異動摘要

Microsoft 的套件 README 以 ``$versionSeries`` 系列彙整版本說明，未提供 ``$Version`` 的逐 build Changelog。以下為本專案依官方內容整理的正體中文摘要：

$formattedVersionNotes

來源：[Microsoft.GameInput $Version](https://www.nuget.org/packages/Microsoft.GameInput/$Version)

## 追版流程

1. 執行 ``pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated`` 確認 NuGet 是否有新版。
2. 執行 ``pwsh ./eng/Show-GameInputGitHubReleases.ps1`` 交叉核對 [microsoftconnect/GameInput](https://github.com/microsoftconnect/GameInput) 的 GitHub Releases——GitHub tag 版號與 NuGet 版號編號機制不同，NuGet 套件說明可能未列出 Release Notes 上的細節，需人工核對後視需要更新 ``eng/gameinput-version-notes.json``，再執行 ``-MarkReviewed`` 標記。
3. 若有新版，執行 ``pwsh ./eng/Update-GameInputVersion.ps1``。
4. 檢查 ``Directory.Packages.props``、``eng/gameinput-baseline.json``、``eng/gameinput-version-notes.json``、``src/InputWeave.GameInput/Interop/Generated/`` 下的 ``.g.cs``、``gameinput-abi-manifest.json`` 與本報告。
5. 執行 ``dotnet build``、``dotnet test``、``pwsh ./eng/Verify-GameInputBindings.ps1``、``pwsh ./eng/Verify-GameInputCoverage.ps1``。
6. 若 GameInput.h 公開 API 有新增或異動，先更新產生器映射，再更新覆蓋率與版本文件。
"@
Write-Utf8NoBomFile -Path (Join-Path $repoRoot 'docs\gameinput-version-report.md') -Content ($report + [Environment]::NewLine)

Write-Information "已更新 Microsoft.GameInput 基準至 $Version。" -InformationAction Continue
