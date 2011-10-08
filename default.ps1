properties {

	$base_dir  = resolve-path .
  	$version = "1.0.0"
  	$release_dir = "$base_dir\Release"

	$productVersion = "3.0"
	$buildNumber = "0";
	$versionFile = "$base_dir\version.txt"
	$packageNameSuffix = "-CI"
	$release_dir = "$base_dir\release"
	$artifacts_dir = "$base_dir\artifacts"
	$tools_dir = "$base_dir\tools"
}

task default -depends CreatePackages


task CreatePackages {
	import-module ./NuGet\packit.psm1
	Write-Output "Loding the moduele for packing.............."
	$packit.push_to_nuget = $false 
	
	
	$packit.framework_Isolated_Binaries_Loc = ".\release"
	$packit.PackagingArtefactsRoot = ".\release\PackagingArtefacts"
	$packit.packageOutPutDir = ".\release\packages"
	$versionFileFullPath = Resolve-Path $versionFile
    $productVersion = Get-Content $versionFileFullPath;
	#Get Build number from TC
	$buildNumber = 0
	if($env:BUILD_NUMBER -ne $null) {
    	$buildNumber = $env:BUILD_NUMBER
	}
	$productVersion = $buildNumber
	
	$packit.targeted_Frameworks = "net40";


	#region Packing NserviceBus
	$packageNameNsb = "NServiceBus" + $packageNameSuffix 
	
	$packit.package_description = "The most popular open-source service bus for .net"
	invoke-packit $packageNameNsb $productVersion @{log4net="1.2.10"} "binaries\NServiceBus.dll", "binaries\NServiceBus.Core.dll"
	#endregion
	
	#region Packing NServiceBus.Host
	$packageName = "NServiceBus.Host" + $packageNameSuffix
	$packit.package_description = "The hosting template for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $productVersion @{$packageNameNsb=$productVersion} "binaries\NServiceBus.Host.exe" 
	#endregion
	
#	#region Packing NServiceBus.Testing
#	$packit.package_description = "The testing for the nservicebus, The most popular open-source service bus for .net"
#	invoke-packit "NServiceBus.Testing" $productVersion @{NServiceBus=$productVersion} "NServiceBus.Testing.dll"
#	#endregion
#	
#	#region Packing NServiceBus.Tools
#	$packit.package_description = "The tools for configure the nservicebus, The most popular open-source service bus for .net"
#	invoke-packit "NServiceBus.Tools" $version @{} "" @{".\tools\msmqutils\*.*"="tools\msmqutils";".\tools\RunMeFirst.bat"="tools";".\tools\install.ps1"="tools"}
#	#endregion
#	
#	#region Packing NServiceBus.Autofac2
#	$packit.package_description = "The Autofac Container for the nservicebus, The most popular open-source service bus for .net"
#	invoke-packit "NServiceBus.Autofac2" $productVersion @{Autofac="2.3.2.632"} "containers\autofac\NServiceBus.ObjectBuilder.Autofac.dll"
#	#endregion
		
	remove-module packit
 }
 
task BuildOnNet35 {
 	
 }
 
task BuildOnNet40 {
 	
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
	
	Write-Output "Build Number: $buildNumber"
	
	$productVersion = $productVersion + "." + $buildNumber
 	Generate-Assembly-Info true "release" "The most popular open-source service bus for .net" "NServiceBus" "NServiceBus" "Copyright © NServiceBus 2007-2011" $productVersion $productVersion ".\src\CommonAssemblyInfo.cs" 
 }

task FinalizeAndClean{
	echo Finalize and Clean
    if((Test-Path -Path $release_dir) -eq $true)
	{
		rmdir $release_dir -Force
	}	
	echo Finalize and Clean
}


task ZipOutput {

	echo "Zip Output"

	$productVersion = Get-Content $versionFileFullPath;
	$buildNumber = 0
	if($env:BUILD_NUMBER -ne $null) {
    	$buildNumber = $env:BUILD_NUMBER
	}
	$productVersion = $buildNumber
	
    $old = pwd
	cd $release_dir
	if((Test-Path -Path $artifacts_dir) -eq $true)
	{
		rmdir $artifacts_dir -Force
	}
	
    mkdir $artifacts_dir
	
	$file = "$artifacts_dir\NServiceBus$productVersion.zip"
	exec { 
		& $tools_dir\zip\zip.exe -9 -A -r $file doc\*.*  *.*
	}

    cd $old
	echo "Zip Output Over"

}
 
function Generate-Assembly-Info{

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
[assembly: AllowPartiallyTrustedCallersAttribute()]
#endif
[assembly: ComVisibleAttribute(false)]
[assembly: CLSCompliantAttribute(true)]
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
 