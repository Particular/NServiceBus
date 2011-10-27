properties {

	$base_dir  = resolve-path .
  	$version = "1.0.0"
  	$release_dir = "$base_dir\Release"

	$productVersion = "3.0"
	$buildNumber = "0";
	$packageNameSuffix = "-CI"
	$release_dir = "$base_dir\release"
	$artifacts_dir = "$base_dir\artifacts"
	$tools_dir = "$base_dir\tools"
}

task default -depends CreatePackages


task CreatePackages {
	import-module ./NuGet\packit.psm1
	Write-Output "Loding the moduele for packing.............."
	$packit.push_to_nuget = $true 
	
	
	$packit.framework_Isolated_Binaries_Loc = ".\release"
	$packit.PackagingArtefactsRoot = ".\release\PackagingArtefacts"
	$packit.packageOutPutDir = ".\release\packages"

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
	invoke-packit $packageNameNsb $productVersion @{log4net="1.2.10"} "binaries\NServiceBus.dll", "binaries\NServiceBus.Core.dll" @{".\src\**\*.cs"="src"} $true;
	#endregion
	
	#region Packing NServiceBus.Host
	$packageName = "NServiceBus.Host" + $packageNameSuffix
	$packit.package_description = "The hosting template for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $productVersion @{$packageNameNsb=$productVersion} "" @{".\release\net40\binaries\NServiceBus.Host32.exe"="lib\net40\x86";".\release\net40\binaries\NServiceBus.Host.exe"="lib\net40\x64"} 
	#endregion
	
	#region Packing NServiceBus.Testing
	$packageName = "NServiceBus.Testing" + $packageNameSuffix
	$packit.package_description = "The testing for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $productVersion @{$packageNameNsb=$productVersion} "binaries\NServiceBus.Testing.dll"
	#endregion
	
	#region Packing NServiceBus.Integration.WebServices
	$packageName = "NServiceBus.Integration.WebServices" + $packageNameSuffix
	$packit.package_description = "The WebServices Integration for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $productVersion @{$packageNameNsb=$productVersion} "binaries\NServiceBus.Integration.WebServices.dll"
	#endregion

	#region Packing NServiceBus.Autofac
	$packageName = "NServiceBus.Autofac" + $packageNameSuffix
	$packit.package_description = "The Autofac Container for the nservicebus"
	invoke-packit $packageName $productVersion @{"Autofac"="2.5.2.830"} "" @{".\release\net40\binaries\containers\autofac\*.*"="lib\net40"}
	#endregion
		
	#region Packing NServiceBus.CastleWindsor
	$packageName = "NServiceBus.CastleWindsor" + $packageNameSuffix
	$packit.package_description = "The CastleWindsor Container for the nservicebus"
	invoke-packit $packageName $productVersion @{"Castle.Core"="3.0.0.2001";"Castle.Windsor"="3.0.0.2001"} "" @{".\release\net40\binaries\containers\castle\*.*"="lib\net40"}
	#endregion
	
	#region Packing NServiceBus.StructureMap
	$packageName = "NServiceBus.StructureMap" + $packageNameSuffix
	$packit.package_description = "The StructureMap Container for the nservicebus"
	invoke-packit $packageName $productVersion @{"structuremap"="2.6.3"} "" @{".\release\net40\binaries\containers\StructureMap\*.*"="lib\net40"}
	#endregion		
	
	#region Packing NServiceBus.Unity
	$packageName = "NServiceBus.Unity" + $packageNameSuffix
	$packit.package_description = "The Unity Container for the nservicebus"
	invoke-packit $packageName $productVersion @{"CommonServiceLocator"="1.0";"Unity"="2.1.505.0";"Unity.Interception"="2.1.505.0"} "" @{".\release\net40\binaries\containers\Unity\*.*"="lib\net40"}
	#endregion
	
	#region Packing NServiceBus.Ninject
	$packageName = "NServiceBus.Ninject" + $packageNameSuffix
	$packit.package_description = "The Ninject Container for the nservicebus"
	invoke-packit $packageName $productVersion @{"Ninject"="2.2.1.4";"Ninject.Extensions.ChildKernel"="2.2.0.5"} "" @{".\release\net40\binaries\containers\Ninject\*.*"="lib\net40"}
	#endregion
	
	#region Packing NServiceBus.Spring
	$packageName = "NServiceBus.Spring" + $packageNameSuffix
	$packit.package_description = "The Spring Container for the nservicebus"
	invoke-packit $packageName $productVersion @{"Common.Logging"="2.0.0";"Spring.Core"="1.3.2"} "" @{".\release\net40\binaries\containers\spring\*.*"="lib\net40"}
	#endregion	
	
	#region Packing NServiceBus.NHibernate
	$packageNameNHibernate = "NServiceBus.NHibernate" + $packageNameSuffix
	$packit.package_description = "The NHibernate for the NServicebus"
	invoke-packit $packageNameNHibernate $productVersion @{"Iesi.Collections"="3.2.0.4000";"NHibernate"="3.2.0.4000"} "binaries\NServiceBus.NHibernate.dll"
	#endregion	
	
	
	#region Packing NServiceBus.Azure
	$packageName = "NServiceBus.Azure" + $packageNameSuffix
	$packit.package_description = "The Azure for the NServicebus"
	invoke-packit $packageName $productVersion @{$packageNameNsb=$productVersion; $packageNameNHibernate=$productVersion} "binaries\NServiceBus.Azure.dll"
	#endregion	
		
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
	$buildNumber = 0
	if($env:BUILD_NUMBER -ne $null) {
    	$buildNumber = $env:BUILD_NUMBER
	}
	Write-Output "Build Number: $buildNumber"
	
	$fileVersion = $productVersion + "." + $buildNumber + ".0"
	$asmVersion =  $productVersion + "0.0"
 	Generate-Assembly-Info true "release" "The most popular open-source service bus for .net" "NServiceBus" "NServiceBus" "Copyright © NServiceBus 2007-2011" $asmVersion $fileVersion ".\src\CommonAssemblyInfo.cs" 
 }

task FinalizeAndClean{
	echo Finalize and Clean
    if(Test-Path -Path $release_dir)
	{
		del -Path $release_dir -Force -recurse
	}	
	echo Finalize and Clean
}


task ZipOutput {

	
	echo "Cleaning the Release dir before ziping"
	
	$packagingArtefacts = ".\release\PackagingArtefacts"
	$packageOutPutDir = ".\release\packages"
	
	if(Test-Path -Path $packagingArtefacts){
		del -Path $packagingArtefacts -Force -recurse
	}
	
	if(Test-Path -Path $packageOutPutDir){
		del -Path $packageOutPutDir -Force -recurse
	}
	
	

	echo "Zip Output"	
	$buildNumber = 0
	if($env:BUILD_NUMBER -ne $null) {
    	$buildNumber = $env:BUILD_NUMBER
	}
	
	$productVersion = $buildNumber
	
	if((Test-Path -Path $artifacts_dir) -eq $true)
	{
		rmdir $artifacts_dir -Force -recurse
	}
	
    mkdir $artifacts_dir
	
	$archive = "$artifacts_dir\NServiceBus.$productVersion.zip"
	exec { 
		& $tools_dir\zip\7za.exe a -tzip $archive $release_dir\**
	}

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
 