properties {
	$ProductVersion = "3.0"
	$BuildNumber = "0";
	$PatchVersion = "0"
	$PreRelease = "-build"	
	$PackageNameSuffix = ""
	$TargetFramework = "net-4.0"
	$UploadPackage = $false;
	$PackageIds = ""
}

$baseDir  = resolve-path .
$releaseRoot = "$baseDir\Release"
$releaseDir = "$releaseRoot\net40"
$binariesDir = "$baseDir\binaries"
$buildBase = "$baseDir\build"
$outDir =  "$buildBase\output"
$coreOnly =  "$buildBase\coreonly"
$libDir = "$baseDir\lib" 
$releaseDir = "$baseDir\release"
$artifactsDir = "$baseDir\artifacts"
$toolsDir = "$baseDir\tools"
$nunitexec = "packages\NUnit.2.5.10.11092\tools\nunit-console.exe"
$nugetExec = "$toolsDir\NuGet\NuGet.exe"
$zipExec = "$toolsDir\zip\7za.exe"
$script:architecture = "x86"
$script:ilmergeTargetFramework = ""
$script:msBuildTargetFramework = ""	
$script:nunitTargetFramework = "/framework=4.0";
$script:msBuild = ""
$script:isEnvironmentInitialized = $false
$script:packageVersion = "3.0.0-local"
$script:releaseVersion = ""

task default -depends PrepareAndReleaseNServiceBus

task CreatePackages {
	
	import-module $baseDir\NuGet\packit.psm1
	Write-Output "Loading the module for packing.............."
	$packit.push_to_nuget = $UploadPackage 
	
	
	$packit.framework_Isolated_Binaries_Loc = "$baseDir\release"
	$packit.PackagingArtifactsRoot = "$baseDir\release\PackagingArtifacts"
	$packit.packageOutPutDir = "$baseDir\release\packages"

	$packit.targeted_Frameworks = "net40";


	#region Packing NServiceBus
	$packageNameNsb = "NServiceBus" + $PackageNameSuffix 
	
	$packit.package_description = "The most popular open-source service bus for .net"
	invoke-packit $packageNameNsb $script:packageVersion @{log4net="1.2.10"} "binaries\NServiceBus.dll", "binaries\NServiceBus.Core.dll" @{} 
	#endregion
	
    #region Packing NServiceBus.Host
	$packageName = "NServiceBus.Host" + $PackageNameSuffix
	$packit.package_description = "The hosting template for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $script:packageVersion @{$packageNameNsb=$script:packageVersion} "" @{".\release\net40\binaries\NServiceBus.Host.*"="lib\net40"}
	#endregion

	#region Packing NServiceBus.Host32
	$packageName = "NServiceBus.Host32" + $PackageNameSuffix
	$packit.package_description = "The hosting template for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $script:packageVersion @{$packageNameNsb=$script:packageVersion} "" @{".\release\net40\binaries\NServiceBus.Host32.*"="lib\net40\x86"}
	#endregion
	
	#region Packing NServiceBus.Testing
	$packageName = "NServiceBus.Testing" + $PackageNameSuffix
	$packit.package_description = "The testing for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $script:packageVersion @{$packageNameNsb=$script:packageVersion} "binaries\NServiceBus.Testing.dll"
	#endregion
	
	#region Packing NServiceBus.Integration.WebServices
	$packageName = "NServiceBus.Integration.WebServices" + $PackageNameSuffix
	$packit.package_description = "The WebServices Integration for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $script:packageVersion @{$packageNameNsb=$script:packageVersion} "binaries\NServiceBus.Integration.WebServices.dll"
	#endregion

	#region Packing NServiceBus.Autofac
	$packageName = "NServiceBus.Autofac" + $PackageNameSuffix
	$packit.package_description = "The Autofac Container for the nservicebus"
	invoke-packit $packageName $script:packageVersion @{"Autofac"="2.5.2.830"} "" @{".\release\net40\binaries\containers\autofac\*.*"="lib\net40"}
	#endregion
		
	#region Packing NServiceBus.CastleWindsor
	$packageName = "NServiceBus.CastleWindsor" + $PackageNameSuffix
	$packit.package_description = "The CastleWindsor Container for the nservicebus"
	invoke-packit $packageName $script:packageVersion @{"Castle.Core"="3.0.0.2001";"Castle.Windsor"="3.0.0.2001"} "" @{".\release\net40\binaries\containers\castle\*.*"="lib\net40"}
	#endregion
	
	#region Packing NServiceBus.StructureMap
	$packageName = "NServiceBus.StructureMap" + $PackageNameSuffix
	$packit.package_description = "The StructureMap Container for the nservicebus"
	invoke-packit $packageName $script:packageVersion @{"structuremap"="2.6.3"} "" @{".\release\net40\binaries\containers\StructureMap\*.*"="lib\net40"}
	#endregion		
	
	#region Packing NServiceBus.Unity
	$packageName = "NServiceBus.Unity" + $PackageNameSuffix
	$packit.package_description = "The Unity Container for the nservicebus"
	invoke-packit $packageName $script:packageVersion @{"CommonServiceLocator"="1.0";"Unity"="2.1.505.0";"Unity.Interception"="2.1.505.0"} "" @{".\release\net40\binaries\containers\Unity\*.*"="lib\net40"}
	#endregion
	
	#region Packing NServiceBus.Ninject
	$packageName = "NServiceBus.Ninject" + $PackageNameSuffix
	$packit.package_description = "The Ninject Container for the nservicebus"
	invoke-packit $packageName $script:packageVersion @{"Ninject"="2.2.1.4";"Ninject.Extensions.ChildKernel"="2.2.0.5"} "" @{".\release\net40\binaries\containers\Ninject\*.*"="lib\net40"}
	#endregion
	
	#region Packing NServiceBus.Spring
	$packageName = "NServiceBus.Spring" + $PackageNameSuffix
	$packit.package_description = "The Spring Container for the nservicebus"
	invoke-packit $packageName $script:packageVersion @{"Common.Logging"="2.0.0";"Spring.Core"="1.3.2"} "" @{".\release\net40\binaries\containers\spring\*.*"="lib\net40"}
	#endregion	
	
	#region Packing NServiceBus.NHibernate
	$packageNameNHibernate = "NServiceBus.NHibernate" + $PackageNameSuffix
	$packit.package_description = "The NHibernate for the NServicebus"
	invoke-packit $packageNameNHibernate $script:packageVersion @{"Iesi.Collections"="3.2.0.4000";"NHibernate"="3.2.0.4000"} "binaries\NServiceBus.NHibernate.dll"
	#endregion	
		
	#region Packing NServiceBus.Azure
	$packageName = "NServiceBus.Azure" + $PackageNameSuffix
	$packit.package_description = "The Azure for the NServicebus"
	invoke-packit $packageName $script:packageVersion @{$packageNameNsb=$script:packageVersion; $packageNameNHibernate=$script:packageVersion; "WindowsAzure.StorageClient.Library"="1.4";"Common.Logging"="2.0.0"} "binaries\NServiceBus.Azure.dll"
	#endregion	
		
	remove-module packit
 }
 
task Clean{

	if(Test-Path $buildBase){
		Delete-Directory $buildBase
		
	}
	
	if(Test-Path $artifactsDir){
		Delete-Directory $artifactsDir
		
	}
	
	if(Test-Path $binariesDir){
		Delete-Directory $binariesDir
		
	}
}

task InitEnvironment{

	if($script:isEnvironmentInitialized -ne $true){
		if ($TargetFramework -eq "net-4.0"){
			$netfxInstallroot ="" 
			$netfxInstallroot =	Get-RegistryValue 'HKLM:\SOFTWARE\Microsoft\.NETFramework\' 'InstallRoot' 
			
			$netfxCurrent = $netfxInstallroot + "v4.0.30319"
			
			$script:msBuild = $netfxCurrent + "\msbuild.exe"
			
			echo ".Net 4.0 build requested - " + $script:msBuild 

			$script:ilmergeTargetFramework  = "/targetplatform:v4," + $netfxCurrent
			
			$script:msBuildTargetFramework ="/p:TargetFrameworkVersion=v4.0 /ToolsVersion:4.0"
			
			$script:nunitTargetFramework = "/framework=4.0";
			
			$script:isEnvironmentInitialized = $true
		}
	
	}
}

task Init -depends Clean, InitEnvironment, InstallDependentPackages, DetectOperatingSystemArchitecture {
   	
	echo "Creating build directory at the follwing path " + $buildBase
	Delete-Directory $buildBase
	Create-Directory $buildBase
	
	$currentDirectory = Resolve-Path .
	
	echo "Current Directory: $currentDirectory" 

	Copy-Item "$libDir\sqlite\*.*"  "$libDir\sqlite\$script:architecture" -Force	
 }
  
task CompileMain -depends Init, GeneateCommonAssemblyInfo { 
 	
 	$solutions = dir "$baseDir\src\core\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\nservicebus\" }
	}
	
	$assemblies = @()
	$assemblies +=	dir $buildBase\nservicebus\NServiceBus.dll
	$assemblies  +=  dir $buildBase\nservicebus\NServiceBus*.dll -Exclude NServiceBus.dll, **Tests.dll

	
	Ilmerge "NServiceBus.snk" $outDir "NServiceBus" $assemblies "dll"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"

	Ilmerge "NServiceBus.snk" $coreOnly "NServiceBus" $assemblies "dll"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"
}

task CompileCore -depends CompileMain, InitEnvironment { 

     $coreDirs = "unicastTransport", "faults", "utils", "ObjectBuilder", "messageInterfaces", "impl\messageInterfaces", "config", "logging", "impl\ObjectBuilder.Common", "installation", "messagemutator", "encryption", "unitofwork", "httpHeaders", "masterNode", "impl\installation", "impl\unicast\NServiceBus.Unicast.Msmq", "impl\Serializers", "unicast", "headers", "impersonation", "impl\unicast\queuing", "impl\unicast\transport", "impl\unicast\NServiceBus.Unicast.Subscriptions.Msmq", "impl\unicast\NServiceBus.Unicast.Subscriptions.InMemory", "impl\faults", "impl\encryption", "databus", "impl\Sagas", "impl\SagaPersisters\InMemory", "impl\SagaPersisters\RavenSagaPersister", "impl\unicast\NServiceBus.Unicast.Subscriptions.Raven", "integration", "impl\databus", "distributor", "gateway", "timeout", "impl\licensing"
	
	$coreDirs | % {
		$solutionDir = Resolve-Path "$baseDir\src\$_"
		cd 	$solutionDir
	 	$solutions = dir "*.sln"
		$solutions | % {
			$solutionFile = $_.FullName
			exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\nservicebus.core\" }
		}
	}
	cd $baseDir
	
	$assemblies  =  dir $buildBase\nservicebus.core\NServiceBus.**.dll -Exclude **Tests.dll 
	
	Ilmerge "NServiceBus.snk" $coreOnly "NServiceBus.Core" $assemblies "dll"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"
	
	$assemblies += dir $buildBase\nservicebus.core\antlr3*.dll	-Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\common.logging.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\common.logging.log4net.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\Interop.MSMQ.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\AutoFac.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\Raven*.dll -Exclude **Tests.dll, Raven.Client.Debug.dll, Raven.Client.MvcIntegration.dll
	$assemblies += dir $buildBase\nservicebus.core\NLog.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\rhino.licensing.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\Newtonsoft.Json.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\ICSharpCode.NRefactory.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\Esent.Interop.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\Lucene.Net.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\Lucene.Net.Contrib.SpellChecker.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\Lucene.Net.Contrib.Spatial.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\nservicebus.core\BouncyCastle.Crypto.dll -Exclude **Tests.dll
	
	Ilmerge "NServiceBus.snk" $outDir "NServiceBus.Core" $assemblies "dll"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"
	
}

task CompileContainers -depends InitEnvironment {

	$solutions = dir "$baseDir\src\impl\ObjectBuilder\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\containers\" }		
	}
	
	Create-Directory "$buildBase\output\containers"
	Copy-Item $buildBase\containers\NServiceBus.ObjectBuilder.**.* $buildBase\output\containers -Force
	Create-Directory $coreOnly\containers
	Copy-Item $buildBase\containers\NServiceBus.ObjectBuilder.**.* $coreOnly\containers -Force
}

task CompileWebServicesIntegration -depends  InitEnvironment{

	$solutions = dir "$baseDir\src\integration\WebServices\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$outDir\" }		
	}
}

task CompileNHibernate -depends InitEnvironment {

	$solutions = dir "$baseDir\src\nhibernate\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\NServiceBus.NHibernate\" }		
	}
	
	$testAssemblies = dir $buildBase\NServiceBus.NHibernate\**Tests.dll
	
#	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework} 
	
	$assemblies = dir $buildBase\NServiceBus.NHibernate\NServiceBus.**NHibernate**.dll -Exclude **Tests.dll
	
	Ilmerge "NServiceBus.snk" $coreOnly "NServiceBus.NHibernate" $assemblies "dll"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"
	
	Ilmerge "NServiceBus.snk" $outDir "NServiceBus.NHibernate" $assemblies "dll"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"
	
}

task CompileAzure -depends InitEnvironment {

	$solutions = dir "$baseDir\src\azure\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\azure\NServiceBus.Azure\" }		
	}
	
	$testAssemblies = dir $buildBase\azure\NServiceBus.Azure\**Tests.dll
	
#	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework} 
	
	$assemblies = dir $buildBase\azure\NServiceBus.Azure\NServiceBus.**Azure**.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\azure\NServiceBus.Azure\NServiceBus.**AppFabric**.dll -Exclude **Tests.dll
	
	Ilmerge "NServiceBus.snk" $coreOnly "NServiceBus.Azure" $assemblies "dll"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"
	
	Ilmerge "NServiceBus.snk" $outDir "NServiceBus.Azure" $assemblies "dll"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"
	
}

task CompileHosts  -depends InitEnvironment {

	if(Test-Path "$buildBase\hosting"){
	
		Delete-Directory "$buildBase\hosting"
	}
	Create-Directory "$buildBase\hosting"
	$solutions = dir "$baseDir\src\hosting\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\hosting\" }		
	}
	
	$assemblies = @("$buildBase\hosting\NServiceBus.Hosting.Windows.exe", "$buildBase\hosting\NServiceBus.Hosting.dll",
		"$buildBase\hosting\Microsoft.Practices.ServiceLocation.dll", "$buildBase\hosting\Magnum.dll", "$buildBase\hosting\Topshelf.dll")
	
	echo "Merging NServiceBus.Host....."	
	Ilmerge "NServiceBus.snk" $outDir\host\ "NServiceBus.Host" $assemblies "exe"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"
	

}

task CompileHosts32  -depends InitEnvironment {		
	$solutions = dir "$baseDir\src\hosting\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\hosting32\" /t:Clean }
		
		exec { &$script:msBuild $solutionFile /p:PlatformTarget=x86 /p:OutDir="$buildBase\hosting32\"}
	}
	
	
	$assemblies = @("$buildBase\hosting32\NServiceBus.Hosting.Windows.exe", "$buildBase\hosting32\NServiceBus.Hosting.dll",
		"$buildBase\hosting32\Microsoft.Practices.ServiceLocation.dll", "$buildBase\hosting32\Magnum.dll", "$buildBase\hosting32\Topshelf.dll")
	
	echo "Merging NServiceBus.Host32....."	
	Ilmerge "NServiceBus.snk" $outDir\host\ "NServiceBus.Host32" $assemblies "exe"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"
	

}

task CompileAzureHosts  -depends InitEnvironment {

	$solutions = dir "$baseDir\src\azure\Hosting\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\azure\Hosting\"}
	}
}

task Test{
	
	if(Test-Path $buildBase\test-reports){
		Delete-Directory $buildBase\test-reports
	}
	
	Create-Directory $buildBase\test-reports 
	
	$testAssemblies = @()
	$testAssemblies +=  dir $buildBase\nservicebus.core\*Tests.dll -Exclude *FileShare.Tests.dll,*Gateway.Tests.dll, *Raven.Tests.dll, *Azure.Tests.dll 
	$testAssemblies +=  dir $buildBase\containers\*Tests.dll -Exclude *FileShare.Tests.dll,*Gateway.Tests.dll, *Raven.Tests.dll, *Azure.Tests.dll 

	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework} 
}

task CompileTools -depends InitEnvironment{
	$toolsDirs = "testing", "claims", "timeout", "azure\timeout", "proxy", "tools\management\Errors\ReturnToSourceQueue\", "utils"
	
	$toolsDirs | % {				
	 	$solutions = dir "$baseDir\src\$_\*.sln"
		$currentOutDir = "$buildBase\$_\"
		$solutions | % {
			$solutionFile = $_.FullName
			exec { &$script:msBuild $solutionFile /p:OutDir="$currentOutDir" }
		}
	}
	
	if(Test-Path $buildBase\tools\MsmqUtils){
		Delete-Directory $buildBase\tools\MsmqUtils
	}
	
	Create-Directory "$buildBase\tools\MsmqUtils"
	Copy-Item $buildBase\utils\*.* $buildBase\tools\MsmqUtils -Force
	Delete-Directory $buildBase\utils
	Copy-Item $buildBase\tools\management\Errors\ReturnToSourceQueue\*.* $buildBase\tools\ -Force
	
	cd $buildBase\tools
	Delete-Directory "management"
	cd $baseDir
	
	
	
	exec {&$nunitexec "$buildBase\testing\NServiceBus.Testing.Tests.dll" $script:nunitTargetFramework} 
		
	$assemblies = @("$buildBase\testing\NServiceBus.Testing.dll", "$buildBase\testing\Rhino.Mocks.dll");
	
	echo "Merging NServiceBus.Testing"	
	Ilmerge "NServiceBus.snk" $outDir\testing "NServiceBus.Testing" $assemblies "dll"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"
	
	
	
	$assemblies = @("$buildBase\nservicebus.core\XsdGenerator.exe",
	"$buildBase\nservicebus.core\NServiceBus.Serializers.XML.dll", 
	"$buildBase\nservicebus.core\NServiceBus.Utils.Reflection.dll")
	
	echo "merging XsdGenerator"	
	Ilmerge "NServiceBus.snk" $buildBase\tools "XsdGenerator" $assemblies "exe"  $script:ilmergeTargetFramework "/internalize:$baseDir\ilmerge.exclude"
}

task PrepareBinaries -depends CompileMain, CompileCore, CompileContainers, CompileWebServicesIntegration, CompileNHibernate, CompileHosts, CompileHosts32, CompileAzure, CompileAzureHosts, CompileTools {
	if(Test-Path $binariesDir){
		Delete-Directory "binaries"
	}
	Create-Directory $binariesDir;
	
	Copy-Item $outDir\NServiceBus*.* $binariesDir -Force;
	Copy-Item $outDir\host\*.* $binariesDir -Force;
	Copy-Item $outDir\testing\*.* $binariesDir -Force;
	
	Copy-Item $libDir\log4net.dll $binariesDir -Force;
	
	
	Create-Directory "$binariesDir\containers\autofac"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Autofac.dll"  $binariesDir\containers\autofac -Force;
	
	Create-Directory "$binariesDir\containers\castle"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.CastleWindsor.dll"  $binariesDir\containers\castle -Force;
	
	Create-Directory "$binariesDir\containers\structuremap"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.StructureMap.dll"  $binariesDir\containers\structuremap -Force;
	
	Create-Directory "$binariesDir\containers\spring"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Spring.dll"  $binariesDir\containers\spring -Force;
			
	Create-Directory "$binariesDir\containers\unity"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Unity.dll"  $binariesDir\containers\unity -Force;		
		
	Create-Directory "$binariesDir\containers\ninject"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Ninject.dll"  $binariesDir\containers\ninject -Force;	
}

task CompileSamples -depends InitEnvironment, PrepareBinaries {

	$samplesDirs = "AsyncPages", "AsyncPagesMvc3", "FullDuplex", "PubSub", "Manufacturing", "GenericHost", "Versioning", "WcfIntegration", "Starbucks", "SendOnlyEndpoint", "DataBus", "Azure\AzureBlobStorageDataBus", "Distributor"
	
	$samplesDirs | % {				
	 	$solutions = dir "$baseDir\Samples\$_\*.sln"
		$solutions | % {
			$solutionFile = $_.FullName
			exec {&$script:msBuild $solutionFile}
		}
	}

}

task PrepareRelease -depends PrepareBinaries, Test, CompileSamples {
	
	if(Test-Path $releaseRoot){
		Delete-Directory $releaseRoot	
	}
	
	Create-Directory $releaseRoot
	if ($TargetFramework -eq "net-4.0"){
		$releaseDir = "$releaseRoot\net40"
	}
	Create-Directory $releaseDir

	 
	Copy-Item -Force "$baseDir\*.txt" $releaseRoot  -ErrorAction SilentlyContinue
	Copy-Item -Force "$baseDir\RunMeFirst.bat" $releaseRoot -ErrorAction  SilentlyContinue
	Copy-Item -Force "$baseDir\RunMeFirst.ps1" $releaseRoot -ErrorAction  SilentlyContinue
	
	Copy-Item -Force -Recurse "$buildBase\tools" $releaseRoot\tools -ErrorAction SilentlyContinue
	
	cd $releaseRoot\tools
	dir -recurse -include ('*.xml', '*.pdb') |ForEach-Object {
	write-host deleting $_ 
	Remove-Item $_ 
	}
	cd $baseDir
	
	Copy-Item -Force -Recurse "$baseDir\docs" $releaseRoot\docs -ErrorAction SilentlyContinue
	
	Copy-Item -Force -Recurse "$baseDir\Samples" $releaseDir\samples  -ErrorAction SilentlyContinue 
	cd $releaseDir\samples 
	
	dir -recurse -include ('bin', 'obj') |ForEach-Object {
	write-host deleting $_ 
	Delete-Directory $_
	}
	cd $baseDir
	
	Copy-Item -Force -Recurse "$baseDir\binaries" $releaseDir\binaries -ErrorAction SilentlyContinue  
	

#		<if test="${include.dependencies != 'true'}">
#			<copy todir="${release.dir}\dependencies" flatten="true">
#				<fileset basedir="${trunk.dir}" >
#					<include name="${core.build.dir}\antlr*.dll" />
#					<include name="${core.build.dir}\common.logging.dll"/>
#					<include name="${lib.dir}\common.logging.log4net.dll"/>
#					<include name="${core.build.dir}\Interop.MSMQ.dll" />
#					<include name="${core.build.dir}\AutoFac.dll"/>
#					<include name="${core.build.dir}\Spring.Core.dll" />
#					<include name="${core.build.dir}\NHibernate*.dll" />
#					<include name="${core.build.dir}\FluentNHibernate.dll" />
#					<include name="${core.build.dir}\Iesi.Collections.dll" />
#					<include name="${core.build.dir}\LinFu*.dll" />
#					<exclude name="${core.build.dir}\**Tests.dll" />
#				</fileset>
#			</copy>
#		</if>
}

task PrepareReleaseWithoutSamples -depends PrepareBinaries, Test{
	
	if(Test-Path $releaseRoot){
		Delete-Directory $releaseRoot	
	}
	
	Create-Directory $releaseRoot
	if ($TargetFramework -eq "net-4.0"){
		$releaseDir = "$releaseRoot\net40"
	}
	Create-Directory $releaseDir

	 
	Copy-Item -Force "$baseDir\*.txt" $releaseRoot  -ErrorAction SilentlyContinue
	Copy-Item -Force "$baseDir\RunMeFirst.bat" $releaseRoot -ErrorAction  SilentlyContinue
	Copy-Item -Force "$baseDir\RunMeFirst.ps1" $releaseRoot -ErrorAction  SilentlyContinue
	
	Copy-Item -Force -Recurse "$buildBase\tools" $releaseRoot\tools -ErrorAction SilentlyContinue
	
	cd $releaseRoot\tools
	dir -recurse -include ('*.xml', '*.pdb') |ForEach-Object {
	write-host deleting $_ 
	Remove-Item $_ 
	}
	cd $baseDir
	
	Copy-Item -Force -Recurse "$baseDir\docs" $releaseRoot\docs -ErrorAction SilentlyContinue
	Copy-Item -Force -Recurse "$baseDir\binaries" $releaseDir\binaries -ErrorAction SilentlyContinue  
	

#		<if test="${include.dependencies != 'true'}">
#			<copy todir="${release.dir}\dependencies" flatten="true">
#				<fileset basedir="${trunk.dir}" >
#					<include name="${core.build.dir}\antlr*.dll" />
#					<include name="${core.build.dir}\common.logging.dll"/>
#					<include name="${lib.dir}\common.logging.log4net.dll"/>
#					<include name="${core.build.dir}\Interop.MSMQ.dll" />
#					<include name="${core.build.dir}\AutoFac.dll"/>
#					<include name="${core.build.dir}\Spring.Core.dll" />
#					<include name="${core.build.dir}\NHibernate*.dll" />
#					<include name="${core.build.dir}\FluentNHibernate.dll" />
#					<include name="${core.build.dir}\Iesi.Collections.dll" />
#					<include name="${core.build.dir}\LinFu*.dll" />
#					<exclude name="${core.build.dir}\**Tests.dll" />
#				</fileset>
#			</copy>
#		</if>
}

<#
This will detect whether the current Operating System is running as a 32-bit or 64-bit Operating System regardless of whether this is a 32-bit or 
64-bit process.
#>
task DetectOperatingSystemArchitecture {
	if (IsWow64 -eq $true)
	{
		$script:architecture = "x64"
	}
    echo "Machine Architecture is " + $script:architecture
}
  
task GeneateCommonAssemblyInfo -depends InstallDependentPackages {
	if($env:BUILD_NUMBER -ne $null) {
    	$BuildNumber = $env:BUILD_NUMBER
	}
	Write-Output "Build Number: $BuildNumber"
	
	$fileVersion = $ProductVersion + "." + $PatchVersion + "." + $BuildNumber 
	$asmVersion =  $ProductVersion + ".0.0"
	$infoVersion = $ProductVersion+ ".0" + $PreRelease + $BuildNumber 
	$script:releaseVersion = $infoVersion
	
	$script:packageVersion = $infoVersion;
	
	Write-Output "##teamcity[buildNumber '$script:releaseVersion']"
	
	Generate-Assembly-Info true "release" "The most popular open-source service bus for .net" "NServiceBus" "NServiceBus" "Copyright � NServiceBus 2007-2011" $asmVersion $fileVersion $infoVersion "$baseDir\src\CommonAssemblyInfo.cs" 
 }
 
task InstallDependentPackages {
 	dir -recurse -include ('packages.config') |ForEach-Object {
	$packageconfig = [io.path]::Combine($_.directory,$_.name)

	write-host $packageconfig 

	 exec{ &$nugetExec install $packageconfig -o packages } 
	}
 }

task PrepareAndReleaseNServiceBus -depends PrepareRelease, CreatePackages, ZipOutput{
    if(Test-Path -Path $releaseDir)
	{
        del -Path $releaseDir -Force -recurse
	}	
	echo "Release completed for NServiceBus." + $script:releaseVersion 
}

task PrepareAndReleaseNServiceBusWithoutSamples -depends PrepareReleaseWithoutSamples, CreatePackages, ZipOutput{
    if(Test-Path -Path $releaseDir)
	{
        del -Path $releaseDir -Force -recurse
	}	
	echo "Release completed for NServiceBus without samples." + $script:releaseVersion 
}
<#Ziping artifacts directory for releasing#>
task ZipOutput {
	
	echo "Cleaning the Release Artifacts before ziping"
	$packagingArtifacts = "$releaseDir\PackagingArtifacts"
	$packageOutPutDir = "$releaseDir\packages"
	
	if(Test-Path -Path $packagingArtifacts ){
		Delete-Directory $packagingArtifacts
	}
	
	if((Test-Path -Path $packageOutPutDir) -and ($UploadPackage) ){
        Delete-Directory $packageOutPutDir
	}

	if((Test-Path -Path $artifactsDir) -eq $true)
	{
		Delete-Directory $artifactsDir
	}
	
    Create-Directory $artifactsDir
	
	$archive = "$artifactsDir\NServiceBus.$script:releaseVersion.zip"
	echo "Ziping artifacts directory for releasing"
	exec { &$zipExec a -tzip $archive $releaseDir\** }
}

task UpdatePackages{
	dir -recurse -include ('packages.config') |ForEach-Object {
		$packageconfig = [io.path]::Combine($_.directory,$_.name)

		write-host $packageconfig

		if($PackageIds -ne "")
		{
			write-host "Doing an unsafe update of" $PackageIds 
			&$nugetExec update $packageconfig -RepositoryPath packages -Id $PackageIds
		}
		else
		{	
			write-host "Doing a safe update of all packages" $PackageIds 
			&$nugetExec update -Safe $packageconfig -RepositoryPath packages
		}
	}
}

function Delete-Directory($directoryName){
	Remove-Item -Force -Recurse $directoryName -ErrorAction SilentlyContinue
}
 
function Create-Directory($directoryName){
	New-Item $directoryName -ItemType Directory | Out-Null
}

function Get-RegistryValues($key) {
  (Get-Item $key -ErrorAction SilentlyContinue).GetValueNames()
}

function Get-RegistryValue($key, $value) {
    (Get-ItemProperty $key $value -ErrorAction SilentlyContinue).$value
}

function AddType{
	Add-Type -TypeDefinition "
	using System;
	using System.Runtime.InteropServices;
	public static class Win32Api
	{
	    [DllImport(""Kernel32.dll"", EntryPoint = ""IsWow64Process"")]
	    [return: MarshalAs(UnmanagedType.Bool)]
	    public static extern bool IsWow64Process(
	        [In] IntPtr hProcess,
	        [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process
	    );
	}
	"
}

function Is64BitOS{
    return (Test-64BitProcess) -or (Test-Wow64)
}

function Is64BitProcess{
    return [IntPtr]::Size -eq 8
}

function IsWow64{
    if ([Environment]::OSVersion.Version.Major -eq 5 -and 
        [Environment]::OSVersion.Version.Major -ge 1 -or 
        [Environment]::OSVersion.Version.Major -ge 6)
    {
		AddType
        $process = [System.Diagnostics.Process]::GetCurrentProcess()
        
        $wow64Process = $false
        
        if ([Win32Api]::IsWow64Process($process.Handle, [ref]$wow64Process) -eq $true)
        {
            return $true
        }
		else
		{
			return $false
		}
    }
    else
    {
        return $false
    }
}
  
function Ilmerge($key, $directory, $name, $assemblies, $extension, $ilmergeTargetframework, $logfilename){    
    new-item -path $directory -name "temp_merge" -type directory -ErrorAction SilentlyContinue
    exec { lib\ilmerge.exe /keyfile:$key /out:"$directory\temp_merge\$name.$extension" $assemblies $ilmergeTargetframework $logfilename}
    Get-ChildItem "$directory\temp_merge\**" -Include *.$extension, *.pdb, *.xml | Copy-Item -Destination $directory
    Remove-Item "$directory\temp_merge" -Recurse -ErrorAction SilentlyContinue
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
	[string]$infoVersion,
	[string]$file = $(throw "file is a required parameter.")
)
	if($infoVersion -eq ""){
		$infoVersion = $fileVersion
	}
	

  $asmInfo = "using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;


[assembly: AssemblyVersion(""$version"")]
[assembly: AssemblyFileVersion(""$fileVersion"")]
[assembly: AssemblyCopyright(""$copyright"")]
[assembly: AssemblyProduct(""$product"")]
[assembly: AssemblyCompany(""$company"")]
[assembly: AssemblyConfiguration(""$configuration"")]
[assembly: AssemblyInformationalVersion(""$infoVersion"")]
[assembly: ComVisible(false)]
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