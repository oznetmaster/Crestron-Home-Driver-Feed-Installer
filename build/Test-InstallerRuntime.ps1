# Copyright (c) 2026 Neil Colvin. Licensed under the MIT License; see LICENSE.
[CmdletBinding()]
param([string]$Root = (Split-Path $PSScriptRoot -Parent), [string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Root = [IO.Path]::GetFullPath($Root)
[xml]$appProject = Get-Content -LiteralPath (Join-Path $Root 'src/CrestronHomeDriverFeedInstaller.App/CrestronHomeDriverFeedInstaller.App.csproj') -Raw
$appVersion = $appProject.SelectSingleNode('/Project/PropertyGroup/Version').InnerText
if ($appVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Application version must contain three numeric parts.' }
foreach ($source in @('installer/Bundle.wxs', 'installer/Product.wxs')) {
    [xml]$sourceXml = Get-Content -LiteralPath (Join-Path $Root $source) -Raw
    $versionNode = $sourceXml.SelectSingleNode('/*/*/@Version')
    if ($null -eq $versionNode -or $versionNode.Value -cne $appVersion) { throw "Installer version differs from application: $source." }
}
if ($env:GITHUB_REF -like 'refs/tags/*' -and $env:GITHUB_REF -cne "refs/tags/v$appVersion") { throw 'Release tag differs from application version.' }
[xml]$versions = Get-Content -LiteralPath (Join-Path $Root 'build/DotNetRuntime.props') -Raw
$required = $versions.Project.PropertyGroup.InstallerDesktopRuntimeVersion
if ([version]$required -lt [version]'10.0.13') { throw 'Installer runtime predates the PKCS security fixes.' }
$publish = Join-Path $Root 'installer/obj/publish'
$appFile = Join-Path $publish 'Crestron Home Driver Feed Installer.dll'
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($appFile).FileVersion -cne "$appVersion.0") { throw 'Published application file version differs from source.' }
$runtime = Get-Content -LiteralPath (Join-Path $publish 'Crestron Home Driver Feed Installer.runtimeconfig.json') -Raw | ConvertFrom-Json
foreach ($name in @('Microsoft.NETCore.App', 'Microsoft.WindowsDesktop.App')) {
    $framework = @($runtime.runtimeOptions.frameworks | Where-Object name -CEQ $name)
    if ($framework.Count -ne 1 -or $framework[0].version -cne $required) { throw "Application minimum shared runtime differs from prerequisite: $name." }
}
$deps = Get-Content -LiteralPath (Join-Path $publish 'Crestron Home Driver Feed Installer.deps.json') -Raw | ConvertFrom-Json -AsHashtable
$ssh = @($deps.libraries.Keys | Where-Object { $_.StartsWith('SSH.NET/', [StringComparison]::Ordinal) })
if ($ssh.Count -ne 1 -or [version]($ssh[0].Split('/')[1]) -lt [version]'2026.0.0') { throw 'Published SSH.NET predates the security fixes.' }
# WPF normally supplies PKCS from the shared runtime. Validate a private copy too if one is present.
foreach ($key in $deps.libraries.Keys | Where-Object { $_.StartsWith('System.Security.Cryptography.Pkcs/', [StringComparison]::Ordinal) }) {
    if ([version]($key.Split('/')[1]) -lt [version]'10.0.13') { throw 'Published PKCS predates the security fixes.' }
}
$project = Join-Path $Root 'installer/CrestronHomeDriverFeedInstaller.Bootstrapper.wixproj'
$wixBin = & dotnet msbuild $project -nologo -getProperty:WixBinDir
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($wixBin)) { throw 'Cannot locate the restored WiX inspection tool.' }
$wix = [IO.Path]::GetFullPath((Join-Path $wixBin '../net472/x64/wix.exe'))
if (!(Test-Path -LiteralPath $wix -PathType Leaf)) { throw 'WiX inspection tool is missing.' }
$bundle = Join-Path $Root "installer/bin/$Configuration/Crestron Home Driver Feed Installer.exe"
$evidence = Join-Path $Root ('artifacts/runtime-prerequisite/' + [Guid]::NewGuid().ToString('N'))
& $wix burn extract $bundle -oba $evidence
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect the compiled bootstrapper.' }
$manifest = [Xml.XmlDocument]::new()
$manifest.XmlResolver = $null
$manifest.Load((Join-Path $evidence 'manifest.xml'))
$namespaces = [Xml.XmlNamespaceManager]::new($manifest.NameTable)
$namespaces.AddNamespace('b', 'http://wixtoolset.org/schemas/v4/2008/Burn')
$package = $manifest.SelectSingleNode('/b:BurnManifest/b:Chain/b:ExePackage[@Id="DotNetDesktopRuntime"]', $namespaces)
$payload = $manifest.SelectSingleNode('/b:BurnManifest/b:Payload[@Id="DotNetDesktopRuntime"]', $namespaces)
$search = $manifest.SelectSingleNode('/b:BurnManifest/b:ExtensionSearch[@Variable="DesktopRuntimeVersion"]', $namespaces)
if ($null -eq $package -or $null -eq $payload -or $null -eq $search) { throw 'Compiled runtime prerequisite or detection search is missing.' }
$extension = [Xml.XmlDocument]::new()
$extension.XmlResolver = $null
$extension.Load((Join-Path $evidence 'BootstrapperExtensionData.xml'))
$extensionNamespaces = [Xml.XmlNamespaceManager]::new($extension.NameTable)
$extensionNamespaces.AddNamespace('e', 'http://wixtoolset.org/schemas/v4/BootstrapperExtensionData')
$searchData = $extension.SelectSingleNode("//e:NetFxNetCoreSearch[@Id='$($search.GetAttribute('Id'))']", $extensionNamespaces)
# These are the desktop/x64 enum values in the pinned WiX 5.0.2 Netfx extension.
if ($null -eq $searchData -or $searchData.GetAttribute('RuntimeType') -cne '2' -or $searchData.GetAttribute('Platform') -cne '1' -or $searchData.GetAttribute('MajorVersion') -cne '10') { throw 'Compiled runtime search must target the .NET 10 x64 Desktop Runtime.' }
if ($package.GetAttribute('DetectCondition') -cne "DesktopRuntimeVersion >= v$required") { throw 'Compiled runtime detection does not enforce the minimum version.' }
$filename = "windowsdesktop-runtime-$required-win-x64.exe"
if ($payload.GetAttribute('FilePath') -cne $filename -or $payload.GetAttribute('DownloadUrl') -cne "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/$required/$filename") { throw 'Compiled runtime payload differs from the application prerequisite.' }
if ($manifest.SelectSingleNode('/b:BurnManifest/b:Chain/b:MsiPackage', $namespaces).GetAttribute('Version') -cne $appVersion) { throw 'Compiled MSI version differs from application.' }
$bundleVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($bundle)
$numericBundleVersion = '{0}.{1}.{2}.{3}' -f $bundleVersion.FileMajorPart, $bundleVersion.FileMinorPart, $bundleVersion.FileBuildPart, $bundleVersion.FilePrivatePart
if ($numericBundleVersion -cne "$appVersion.0" -or $manifest.SelectSingleNode('/b:BurnManifest/b:Registration', $namespaces).GetAttribute('Version') -cne $appVersion) { throw 'Compiled bootstrapper version differs from application.' }
@{ applicationVersion=$appVersion; requiredRuntime=$required; sshPackage=$ssh[0]; compiledRuntimeDetection=$package.GetAttribute('DetectCondition'); bundleSha256=(Get-FileHash -LiteralPath $bundle -Algorithm SHA256).Hash } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'verification.json') -Encoding utf8
Write-Output "Verified application minimum runtime, SSH dependency, and compiled bootstrapper prerequisite: .NET $required."
