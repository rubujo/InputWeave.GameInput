#requires -Version 7.4

<#
.SYNOPSIS
驗證 README 與 docs 內的 C# 範例都能實際編譯。

.DESCRIPTION
每個 ```csharp 區塊都視為一個完整的頂層程式（top-level statements），放進參考本程式庫的主控台專案中，預設同時以 net48、net8.0 與 net10.0 編譯。
只適用部分目標框架的範例可在語言標記後宣告，例如 ```csharp tfm=net8.0;net10.0；GitHub 只以第一個字判斷語法醒目提示，不影響呈現。
專案關閉隱式 using，確保範例自行宣告所需的 using，並把可為 null 的警告視為錯誤；範例與公開 API 不一致時會列出檔案、區塊序號與編譯錯誤並失敗。
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

$referenceAssembliesVersion = @($packagesProps.Project.ItemGroup.PackageVersion | Where-Object { $_.Include -eq 'Microsoft.NETFramework.ReferenceAssemblies.net48' })[0].Version
if ([string]::IsNullOrWhiteSpace($referenceAssembliesVersion))
{
    throw 'Directory.Packages.props 缺少 Microsoft.NETFramework.ReferenceAssemblies.net48 版本。'
}

$documents = @(Join-Path $repoRoot 'README.md') + @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'docs') -Filter '*.md' -File | Sort-Object Name | ForEach-Object FullName)
$snippets = [System.Collections.Generic.List[object]]::new()
foreach ($document in $documents)
{
    $content = Get-Content -LiteralPath $document -Raw -Encoding utf8
    $index = 0
    foreach ($match in [System.Text.RegularExpressions.Regex]::Matches($content, '(?s)```csharp(?:[ \t]+tfm=(?<tfm>[^\r\n]+))?\r?\n(?<code>.*?)```'))
    {
        $index++
        [string[]]$targetFrameworks = @()
        if ($match.Groups['tfm'].Success)
        {
            $targetFrameworks = @($match.Groups['tfm'].Value.Trim() -split ';' | Where-Object { $_ })
        }

        $snippets.Add([pscustomobject]@{
                Document = [System.IO.Path]::GetRelativePath($repoRoot, $document)
                Index = $index
                TargetFrameworks = $targetFrameworks
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
    <TargetFrameworks>net48;net8.0;net10.0</TargetFrameworks>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <WarningsAsErrors>nullable</WarningsAsErrors>
    <ImplicitUsings>disable</ImplicitUsings>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="$dependencyInjectionVersion" />
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies.net48" Version="$referenceAssembliesVersion" PrivateAssets="all" Condition="'`$(TargetFramework)' == 'net48'" />
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
    # 未宣告 tfm= 時一次建置全部目標框架；有宣告時逐一以 -f 建置指定的目標框架。
    $frameworks = if ($snippet.TargetFrameworks.Count -gt 0) { $snippet.TargetFrameworks } else { @('') }
    $output = [System.Collections.Generic.List[string]]::new()
    $buildFailed = $false
    foreach ($framework in $frameworks)
    {
        $buildArguments = @('build', $workDirectory, '-c', 'Release', '--nologo', '-v', 'q')
        if ($framework)
        {
            $buildArguments += @('-f', $framework)
        }

        if ($restored)
        {
            $buildArguments += '--no-restore'
        }

        foreach ($line in @(& dotnet @buildArguments 2>&1))
        {
            $output.Add("$line")
        }

        if ($LASTEXITCODE -ne 0)
        {
            $buildFailed = $true
        }

        $restored = $true
    }

    $errors = @($output | Where-Object { $_ -match 'error (CS|NU)\d+' } | ForEach-Object { ($_ -replace '^.*?error ', '') -replace '\s*\[[^\]]*\]\s*$', '' } | Sort-Object -Unique)
    if ($buildFailed -or $errors.Count -gt 0)
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

Write-Information "文件 C# 範例編譯驗證通過：共 $($snippets.Count) 個範例（預設 net48、net8.0 與 net10.0）。" -InformationAction Continue
