#requires -Version 7.4

[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+(\.\d+){1,2}$')]
    [string]$Version
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

$headerPath = Join-Path $extractPath 'native\include\GameInput.h'
$redistPath = Join-Path $extractPath 'redist\GameInputRedist.msi'
if (-not (Test-Path -LiteralPath $headerPath -PathType Leaf))
{
    throw '下載的 NuGet 套件缺少 native/include/GameInput.h。'
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
  "apiVersion": 3,
  "nupkgSha256": "$nupkgHash",
  "headerPath": "native/include/GameInput.h",
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

    if ($relativePath -eq 'docs\gameinput-api-coverage.md')
    {
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

- API 版本：``3``
- NuGet 套件 SHA256：``$nupkgHash``
- ``native/include/GameInput.h`` SHA256：``$headerHash``
- ``redist/GameInputRedist.msi`` SHA256：``$redistHash``

低階互通層來源：``src/InputWeave.GameInput/Interop/Generated/`` 下的列舉、常數、HRESULT、IID、回呼委派、結構配置、COM 介面與 ``gameinput-abi-manifest.json`` 均由目前基準的 ``GameInput.h`` 產生。

## 追版流程

1. 執行 ``pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated`` 確認 NuGet 是否有新版。
2. 若有新版，執行 ``pwsh ./eng/Update-GameInputVersion.ps1``。
3. 檢查 ``Directory.Packages.props``、``eng/gameinput-baseline.json``、``src/InputWeave.GameInput/Interop/Generated/`` 下的 ``.g.cs``、``gameinput-abi-manifest.json`` 與本報告。
4. 執行 ``dotnet build``、``dotnet test``、``pwsh ./eng/Verify-GameInputBindings.ps1``、``pwsh ./eng/Verify-GameInputCoverage.ps1``。
5. 若 GameInput.h 公開 API 有新增或異動，先更新產生器映射，再更新覆蓋率與版本文件。
"@
Write-Utf8NoBomFile -Path (Join-Path $repoRoot 'docs\gameinput-version-report.md') -Content ($report + [Environment]::NewLine)

Write-Information "已更新 Microsoft.GameInput 基準至 $Version。" -InformationAction Continue
