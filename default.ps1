properties {
	$productVersion = "5.0"
	$buildNumber = "0";
	$versionFile = ".\version.txt"
	
}


task default -depends CreatePackages


task CreatePackages -depends InstallDependentPackages, GeneateCommonAssemblyInfo, BuildOnNet35, BuildOnNet40 {
#	import-module ./NuGet\packit.psm1
#	Write-Output "Loding the moduele for packing.............."
#	$packit.push_to_nuget = $false 
#	
#	
#	$packit.framework_Isolated_Binaries_Loc = ".\outdir\lib"
#	
#	$versionFileFullPath = Resolve-Path $versionFile
#    $productVersion = Get-Content $versionFileFullPath;
#	#Get Build number from TC
#	$buildNumber = 0
#	if($env:BUILD_NUMBER -ne $null) {
#    	$buildNumber = $env:BUILD_NUMBER
#	}
#	$productVersion = $productVersion + "." + $buildNumber
#
#	#region Packing NserviceBus
#	$packit.package_description = "The most popular open-source service bus for .net"
#	invoke-packit "NServiceBus" $productVersion @{log4net="1.2.10"} "NServiceBus.dll", "NServiceBus.Core.dll","NServiceBus.pdb","NServiceBus.Core.pdb" @{".\src\core\NServiceBus\*.cs"="src\core\NServiceBus";".\src\core\NServiceBus\Properties\*cs"="src\core\NServiceBus\Properties"}
#	#endregion
#	
#	#region Packing NServiceBus.Host
#	$packit.package_description = "The hosting template for the nservicebus, The most popular open-source service bus for .net"
#	invoke-packit "NServiceBus.Host" $productVersion @{NServiceBus=$productVersion} "NServiceBus.Host.exe" 
#	#endregion
#	
#	#region Packing NServiceBus.Testing
#	$packit.package_description = "The testing for the nservicebus, The most popular open-source service bus for .net"
#	invoke-packit "NServiceBus.Testing" $productVersion @{NServiceBus=$productVersion} "NServiceBus.Testing.dll"
#	#endregion
	
#	#region Packing NServiceBus.Tools
#	$packit.package_description = "The tools for configure the nservicebus, The most popular open-source service bus for .net"
#	invoke-packit "NServiceBus.Tools" $version @{} "" @{".\tools\msmqutils\*.*"="tools\msmqutils";".\tools\RunMeFirst.bat"="tools";".\tools\install.ps1"="tools"}
#	#endregion
	
#	#region Packing NServiceBus.Autofac2
#	$packit.package_description = "The Autofac Container for the nservicebus, The most popular open-source service bus for .net"
#	invoke-packit "NServiceBus.Autofac2" $productVersion @{Autofac="2.3.2.632"} "containers\autofac\NServiceBus.ObjectBuilder.Autofac.dll"
#	#endregion
	
#	remove-module packit
 }
 
task BuildOnNet35 {
# 	.\tools\nant\nant.exe -D:targetframework=net-3.5
#	XCopy  .\binaries\* .\outdir\lib\net35\ /S /Y
 }
 
task BuildOnNet40 {
# 	.\tools\nant\nant.exe -D:targetframework=net-4.0
#	XCopy  .\binaries\* .\outdir\lib\net40\ /S /Y
 } 
 
 task InstallDependentPackages {
 	dir -recurse -include ('packages.config') |ForEach-Object {
	$packageconfig = [io.path]::Combine($_.directory,$_.name)

	write-host $packageconfig 

	.\tools\NuGet\NuGet.exe install $packageconfig -o packages 
	}
 }
 
 task GeneateCommonAssemblyInfo {
    $versionFileFullPath = Resolve-Path $versionFile
    $productVersion = Get-Content $versionFileFullPath;
	$buildNumber = 0
	if($env:BUILD_NUMBER -ne $null) {
    	$buildNumber = $env:BUILD_NUMBER
	}
	
	$productVersion = $productVersion + "." + $buildNumber
 	Generate-Assembly-Info true "release" "The most popular open-source service bus for .net" "NServiceBus" "NServiceBus" "Copyright © NServiceBus 2007-2011" $productVersion $productVersion ".\src\CommonAssemblyInfo.cs" 
 }
 
 function Generate-Assembly-Info
{
param(
	[string]$clsCompliant = "true",
	[string]$configuration, 
	[string]$description, 
	[string]$company, 
	[string]$product, 
	[string]$copyright, 
	[string]$version,
	[string]$fileVersion,
	[string]$file = $(throw "file is a required parameter.")
)
  $asmInfo = "using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

[assembly: AssemblyVersionAttribute(""$version"")]
[assembly: AssemblyFileVersionAttribute(""$fileVersion"")]
[assembly: AssemblyCopyrightAttribute(""$copyright"")]
[assembly: AssemblyProductAttribute(""$product"")]
[assembly: AssemblyCompanyAttribute(""$company"")]
[assembly: AssemblyConfigurationAttribute(""$configuration"")]
[assembly: AssemblyInformationalVersionAttribute(""$fileVersion"")]
#if NET35
[assembly: AllowPartiallyTrustedCallersAttribute(true)]
#endif
[assembly: ComVisibleAttribute(false)]
"

	$dir = [System.IO.Path]::GetDirectoryName($file)
	
	if ([System.IO.Directory]::Exists($dir) -eq $false)
	{
		Write-Host "Creating directory $dir"
		[System.IO.Directory]::CreateDirectory($dir)
	}
	Write-Host "Generating assembly info file: $file"
	Write-Output $asmInfo > $file
}
 