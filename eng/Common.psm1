#requires -Version 7.4

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-RepoRoot
{
    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Get-GameInputPackageVersion
{
    $repoRoot = Get-RepoRoot
    $propsPath = Join-Path $repoRoot 'Directory.Packages.props'
    [xml]$props = Get-Content -LiteralPath $propsPath -Raw -Encoding utf8
    $node = $props.Project.ItemGroup.PackageVersion | Where-Object { $_.Include -eq 'Microsoft.GameInput' } | Select-Object -First 1

    if ($null -eq $node)
    {
        throw 'Directory.Packages.props 找不到 Microsoft.GameInput 版本。'
    }

    return [string]$node.Version
}

function Get-GameInputPackageRoot
{
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Version
    )

    $packageRoot = Join-Path $HOME ".nuget\packages\microsoft.gameinput\$Version"
    if (-not (Test-Path -LiteralPath $packageRoot -PathType Container))
    {
        throw "找不到 Microsoft.GameInput $Version 的 NuGet 快取。請先執行 dotnet restore。"
    }

    return (Resolve-Path $packageRoot).Path
}

function Get-Sha256
{
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Path
    )

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
}

function Write-Utf8NoBomFile
{
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Path,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$Content,

        [ValidateSet('CRLF', 'LF')]
        [string]$NewLine = 'CRLF'
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $directory = [System.IO.Path]::GetDirectoryName($fullPath)
    if (-not [string]::IsNullOrWhiteSpace($directory))
    {
        [System.IO.Directory]::CreateDirectory($directory) | Out-Null
    }

    $normalized = $Content -replace "`r`n", "`n"
    $normalized = $normalized -replace "`r", "`n"
    if ($NewLine -eq 'CRLF')
    {
        $normalized = $normalized -replace "`n", "`r`n"
    }

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($fullPath, $normalized, $encoding)
}

function Test-HasUtf8Bom
{
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf))
    {
        return $false
    }

    $bytes = [System.IO.File]::ReadAllBytes([System.IO.Path]::GetFullPath($Path))
    return $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
}

function Write-Utf8PreservingBomFile
{
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Path,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$Content,

        [ValidateSet('CRLF', 'LF')]
        [string]$NewLine = 'CRLF'
    )

    $emitBom = Test-HasUtf8Bom -Path $Path
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $directory = [System.IO.Path]::GetDirectoryName($fullPath)
    if (-not [string]::IsNullOrWhiteSpace($directory))
    {
        [System.IO.Directory]::CreateDirectory($directory) | Out-Null
    }

    $normalized = $Content -replace "`r`n", "`n"
    $normalized = $normalized -replace "`r", "`n"
    if ($NewLine -eq 'CRLF')
    {
        $normalized = $normalized -replace "`n", "`r`n"
    }

    $encoding = [System.Text.UTF8Encoding]::new($emitBom)
    [System.IO.File]::WriteAllText($fullPath, $normalized, $encoding)
}

function Get-LatestGameInputVersion
{
    $index = Invoke-RestMethod -Uri 'https://api.nuget.org/v3-flatcontainer/microsoft.gameinput/index.json'
    $stableVersions = @($index.versions | Where-Object { $_ -notmatch '-' })
    if ($stableVersions.Count -eq 0)
    {
        throw 'NuGet 查詢不到 Microsoft.GameInput 穩定版。'
    }

    return ($stableVersions | Sort-Object { [System.Version]$_ } | Select-Object -Last 1)
}

Export-ModuleMember -Function Get-RepoRoot, Get-GameInputPackageVersion, Get-GameInputPackageRoot, Get-Sha256, Write-Utf8NoBomFile, Test-HasUtf8Bom, Write-Utf8PreservingBomFile, Get-LatestGameInputVersion
