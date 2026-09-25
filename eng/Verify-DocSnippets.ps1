#requires -Version 7.4

<#
.SYNOPSIS
驗證 README 與 docs 內的 C# 範例都能實際編譯。

.DESCRIPTION
每個 ```csharp 區塊都視為一個完整的頂層程式（top-level statements），放進參考本程式庫的主控台專案中編譯。
專案關閉隱式 using，確保範例自行宣告所需的 using；範例與公開 API 不一致時會列出檔案、區塊序號與編譯錯誤並失敗。
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force

$repoRoot = Get-RepoRoot
$libraryProject = Join-Path $repoRoot 'src\InputWeave.GameInput\InputWeave.GameInput.csproj'
$packagesProps = [xml](Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Packages.props') -Raw -Encoding utf8)
$dependencyInjectionVersion = @($packagesProps.Project.ItemGroup.PackageVersion | Where-Object { $_.Include -eq 'Microsoft.Extensions.DependencyInjection' })[0].Version
if ([string]::IsNullOrWhiteSpace($dependencyInjectionVersion))
{
    throw 'Directory.Packages.props 缺少 Microsoft.Extensions.DependencyInjection 版本。'
}

$documents = @(Join-Path $repoRoot 'README.md') + @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'docs') -Filter '*.md' -File | Sort-Object Name | ForEach-Object FullName)
$snippets = [System.Collections.Generic.List[object]]::new()
foreach ($document in $documents)
{
    $content = Get-Content -LiteralPath $document -Raw -Encoding utf8
    $index = 0
    foreach ($match in [System.Text.RegularExpressions.Regex]::Matches($content, '(?s)```csharp\r?\n(?<code>.*?)```'))
    {
        $index++
        $snippets.Add([pscustomobject]@{
                Document = [System.IO.Path]::GetRelativePath($repoRoot, $document)
                Index = $index
                Code = $match.Groups['code'].Value
            })
    }
}

if ($snippets.Count -eq 0)
{
    throw '找不到任何 C# 範例，請確認文件路徑。'
}

$workDirectory = Join-Path ([System.IO.Path]::GetTempPath()) 'inputweave-doc-snippets'
New-Item -ItemType Directory -Force -Path $workDirectory | Out-Null
$projectContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="$dependencyInjectionVersion" />
    <ProjectReference Include="$libraryProject" />
  </ItemGroup>
</Project>
"@
Write-Utf8NoBomFile -Path (Join-Path $workDirectory 'DocSnippet.csproj') -Content $projectContent
$programPath = Join-Path $workDirectory 'Program.cs'

$failures = [System.Collections.Generic.List[string]]::new()
$restored = $false
foreach ($snippet in $snippets)
{
    Write-Utf8NoBomFile -Path $programPath -Content $snippet.Code
    $buildArguments = @('build', $workDirectory, '-c', 'Release', '--nologo', '-v', 'q')
    if ($restored)
    {
        $buildArguments += '--no-restore'
    }

    $output = & dotnet @buildArguments 2>&1
    $restored = $true
    $errors = @($output | ForEach-Object { "$_" } | Where-Object { $_ -match 'error (CS|NU)\d+' } | ForEach-Object { ($_ -replace '^.*?error ', '') -replace '\s*\[[^\]]*\]\s*$', '' } | Sort-Object -Unique)
    if ($LASTEXITCODE -ne 0 -or $errors.Count -gt 0)
    {
        $detail = if ($errors.Count -gt 0) { $errors -join [Environment]::NewLine + '    ' } else { '建置失敗，未取得編譯錯誤訊息。' }
        $failures.Add("$($snippet.Document) 第 $($snippet.Index) 個 C# 範例無法編譯：$([Environment]::NewLine)    $detail")
    }
}

if ($failures.Count -gt 0)
{
    foreach ($failure in $failures)
    {
        Write-Error $failure -ErrorAction Continue
    }

    throw "文件 C# 範例編譯驗證失敗，共 $($failures.Count) 個。"
}

Write-Information "文件 C# 範例編譯驗證通過：共 $($snippets.Count) 個範例。" -InformationAction Continue
