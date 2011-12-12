properties {

	
	$productVersion = "3.0"
	$buildNumber = "0";
	$patchVersion = "0"
	$preRelease = "+build"	
	$packageNameSuffix = ""
	
	
	$targetframework = "net-4.0"
	
	$uploadPackage = $false;
	
	$packageIds = ""
	
		
}

$base_dir  = resolve-path .
$release_root = "$base_dir\Release"
$release_dir = "$release_root\net40"
$binaries_dir = "$base_dir\binaries"
$build_base = "$base_dir\build"
$outDir =  "$build_base\output"
$coreOnly =  "$build_base\coreonly"
$libDir = "$base_dir\lib" 
$release_dir = "$base_dir\release"
$artifacts_dir = "$base_dir\artifacts"
$tools_dir = "$base_dir\tools"
$nunitexec = "packages\NUnit.2.5.10.11092\tools\nunit-console.exe"
$nugetExec = "$tools_dir\NuGet\NuGet.exe"
$zipExec = "$tools_dir\zip\7za.exe"
$script:architecture = "x86"
$script:ilmergeTargetframework = ""
$script:msbuildTargetFramework = ""	
$script:nunitTargetFramework = "/framework=4.0";
$script:msbuild = ""
$script:isEnviormentinitialized = $false
$script:packageVersion = "3.0.0"
$script:releaseVersion = ""

task default -depends prepare_and_release_nservicebus

task create_packages {
	import-module $base_dir\NuGet\packit.psm1
	
	Write-Output "Loading the module for packing.............."
	$packit.push_to_nuget = $uploadPackage 
	
	
	$packit.framework_Isolated_Binaries_Loc = "$base_dir\release"
	$packit.PackagingArtifactsRoot = "$base_dir\release\PackagingArtifacts"
	$packit.packageOutPutDir = "$base_dir\release\packages"

	#Get Build number from TC
	
	if(($packageNameSuffix -eq "") -and ($preRelease -eq "+build")){
		$packageNameSuffix = "-CI"
	}
	
	
	$productVersion = $script:packageVersion;
	
	$packit.targeted_Frameworks = "net40";


	#region Packing NServiceBus
	$packageNameNsb = "NServiceBus" + $packageNameSuffix 
	
	$packit.package_description = "The most popular open-source service bus for .net"
	invoke-packit $packageNameNsb $productVersion @{log4net="1.2.10"} "binaries\NServiceBus.dll", "binaries\NServiceBus.pdb", "binaries\NServiceBus.Core.dll", "binaries\NServiceBus.Core.pdb" @{} @(@{"src"="..\..\..\src\**\*.cs";"target"="src\src";"exclude"="*.sln;*.csproj;*.config;*.cache"}) $true;
	#endregion
	
    #region Packing NServiceBus.Host
	$packageName = "NServiceBus.Host" + $packageNameSuffix
	$packit.package_description = "The hosting template for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $productVersion @{$packageNameNsb=$productVersion} "" @{".\release\net40\binaries\NServiceBus.Host.*"="lib\net40"} $null $true 
	#endregion

	#region Packing NServiceBus.Host32
	$packageName = "NServiceBus.Host32" + $packageNameSuffix
	$packit.package_description = "The hosting template for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $productVersion @{$packageNameNsb=$productVersion} "" @{".\release\net40\binaries\NServiceBus.Host32.*"="lib\net40\x86"} $null $true 
	#endregion
	
	#region Packing NServiceBus.Testing
	$packageName = "NServiceBus.Testing" + $packageNameSuffix
	$packit.package_description = "The testing for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $productVersion @{$packageNameNsb=$productVersion} "binaries\NServiceBus.Testing.dll", "binaries\NServiceBus.Testing.pdb" @{} $null $true
	#endregion
	
	#region Packing NServiceBus.Integration.WebServices
	$packageName = "NServiceBus.Integration.WebServices" + $packageNameSuffix
	$packit.package_description = "The WebServices Integration for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $productVersion @{$packageNameNsb=$productVersion} "binaries\NServiceBus.Integration.WebServices.dll", "binaries\NServiceBus.Integration.WebServices.pdb" @{} $null $true
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
	invoke-packit $packageName $productVersion @{$packageNameNsb=$productVersion; $packageNameNHibernate=$productVersion; "WindowsAzure.StorageClient.Library"="1.4";"Common.Logging"="2.0.0"} "binaries\NServiceBus.Azure.dll"
	#endregion	
		
	remove-module packit
 }
 
task clean{

	if(Test-Path $build_base){
		delete_directory $build_base
		
	}
	
	if(Test-Path $artifacts_dir){
		delete_directory $artifacts_dir
		
	}
	
	if(Test-Path $binaries_dir){
		delete_directory $binaries_dir
		
	}
}

task init_enviorment{

	if($script:isEnviormentinitialized -ne $true){
		if ($targetframework -eq "net-4.0"){
			$netfxInstallroot ="" 
			$netfxInstallroot =	Get-RegistryValue 'HKLM:\SOFTWARE\Microsoft\.NETFramework\' 'InstallRoot' 
			echo "Netfx in: " + $netfxInstallroot 

			$netfxCurrent = $netfxInstallroot + "v4.0.30319"
			
			$script:msbuild = $netfxCurrent + "\msbuild.exe"
			
			echo ".Net 4.0 build requested - " + $script:msbuild 

			$script:ilmergeTargetframework  = "/targetplatform:v4," + $netfxCurrent
			
			$script:msbuildTargetFramework ="/p:TargetFrameworkVersion=v4.0 /ToolsVersion:4.0"
			
			$script:nunitTargetFramework = "/framework=4.0";
			
			$script:isEnviormentinitialized = $true
		}
	
	}
}

task init -depends clean, init_enviorment, install_dependent_packages, detect_operating_system_architecture {
   	$datetimeBuildtime  = [datetime]::Now
	echo "Creating build dir" + $build_base
	delete_directory $build_base
	create_directory $build_base
	
	$currentDirectory = Resolve-Path .
	
	echo "Current Directory: $currentDirectory" 

	echo $script:architecture
	
	Copy-Item "$libDir\sqlite\*.*"  "$libDir\sqlite\$script:architecture" -Force	
 }
  
task compile_main -depends init, geneate_common_assembly_info { 
 	
 	$solutions = dir "$base_dir\src\core\*.sln"
	$solutions | % {
		$solution_file = $_.FullName
		exec { &$script:msbuild $solution_file /p:OutDir="$build_base\nservicebus\" }
	}
	
	$assemblies  =  dir $build_base\nservicebus\NServiceBus*.dll
	

	Ilmerge "NServiceBus.snk" $outDir "NServiceBus" $assemblies "dll"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
	Ilmerge "NServiceBus.snk" $coreOnly "NServiceBus" $assemblies "dll"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
}

task compile_core -depends compile_main, init_enviorment { 

     $core_dirs = "unicastTransport","faults","utils","ObjectBuilder","messageInterfaces","impl\messageInterfaces","config","logging","impl\ObjectBuilder.Common","installation","messagemutator","encryption","unitofwork","httpHeaders","masterNode","impl\installation","impl\unicast\NServiceBus.Unicast.Msmq","impl\Serializers","unicast","headers","impersonation","impl\unicast\queuing","impl\unicast\transport","impl\unicast\NServiceBus.Unicast.Subscriptions.Msmq","impl\unicast\NServiceBus.Unicast.Subscriptions.InMemory","impl\faults","impl\encryption","databus","impl\Sagas","impl\master","impl\SagaPersisters\InMemory","impl\SagaPersisters\RavenSagaPersister","impl\unicast\NServiceBus.Unicast.Subscriptions.Raven","integration","impl\databus","distributor","gateway","timeout","impl\licensing"
	
	$core_dirs | % {
		$solutionDir = Resolve-Path "$base_dir\src\$_"
		cd 	$solutionDir
	 	$solutions = dir "*.sln"
		$solutions | % {
			$solution_file = $_.FullName
			exec { &$script:msbuild $solution_file /p:OutDir="$build_base\nservicebus.core\" }
		}
	}
	cd $base_dir
	
	$assemblies  =  dir $build_base\nservicebus.core\NServiceBus.**.dll -Exclude **Tests.dll 
	
	Ilmerge "NServiceBus.snk" $coreOnly "NServiceBus.Core" $assemblies "dll"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
	
	$assemblies += dir $build_base\nservicebus.core\antlr3*.dll	-Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\common.logging.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\common.logging.log4net.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\Interop.MSMQ.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\AutoFac.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\Raven*.dll -Exclude **Tests.dll, Raven.Client.Debug.dll, Raven.Client.MvcIntegration.dll
	$assemblies += dir $build_base\nservicebus.core\NLog.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\rhino.licensing.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\Newtonsoft.Json.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\ICSharpCode.NRefactory.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\Esent.Interop.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\Lucene.Net.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\Lucene.Net.Contrib.SpellChecker.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\Lucene.Net.Contrib.Spatial.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\nservicebus.core\BouncyCastle.Crypto.dll -Exclude **Tests.dll
	
	Ilmerge "NServiceBus.snk" $outDir "NServiceBus.Core" $assemblies "dll"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
	
}

task compile_containers -depends init_enviorment {

	$solutions = dir "$base_dir\src\impl\ObjectBuilder\*.sln"
	$solutions | % {
		$solution_file = $_.FullName
		exec { &$script:msbuild $solution_file /p:OutDir="$build_base\containers\" }		
	}
	
	create_directory "$build_base\output\containers"
	
	Copy-Item $build_base\containers\NServiceBus.ObjectBuilder.**.* $build_base\output\containers -Force
	
	create_directory $coreOnly\containers
	
	Copy-Item $build_base\containers\NServiceBus.ObjectBuilder.**.* $coreOnly\containers -Force
}

task compile_webservices_integration -depends  init_enviorment{

	$solutions = dir "$base_dir\src\integration\WebServices\*.sln"
	$solutions | % {
		$solution_file = $_.FullName
		exec { &$script:msbuild $solution_file /p:OutDir="$outDir\" }		
	}
}

task compile_nhibernate -depends init_enviorment {

	$solutions = dir "$base_dir\src\nhibernate\*.sln"
	$solutions | % {
		$solution_file = $_.FullName
		exec { &$script:msbuild $solution_file /p:OutDir="$build_base\NServiceBus.NHibernate\" }		
	}
	
	$testAssemblies = dir $build_base\NServiceBus.NHibernate\**Tests.dll
	
#	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework} 
	
	$assemblies = dir $build_base\NServiceBus.NHibernate\NServiceBus.**NHibernate**.dll -Exclude **Tests.dll
	
	Ilmerge "NServiceBus.snk" $coreOnly "NServiceBus.NHibernate" $assemblies "dll"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
	
	Ilmerge "NServiceBus.snk" $outDir "NServiceBus.NHibernate" $assemblies "dll"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
	
}

task compile_azure -depends init_enviorment {

	$solutions = dir "$base_dir\src\azure\*.sln"
	$solutions | % {
		$solution_file = $_.FullName
		exec { &$script:msbuild $solution_file /p:OutDir="$build_base\azure\NServiceBus.Azure\" }		
	}
	
	$testAssemblies = dir $build_base\azure\NServiceBus.Azure\**Tests.dll
	
#	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework} 
	
	$assemblies = dir $build_base\azure\NServiceBus.Azure\NServiceBus.**Azure**.dll -Exclude **Tests.dll
	$assemblies += dir $build_base\azure\NServiceBus.Azure\NServiceBus.**AppFabric**.dll -Exclude **Tests.dll
	
	Ilmerge "NServiceBus.snk" $coreOnly "NServiceBus.Azure" $assemblies "dll"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
	
	Ilmerge "NServiceBus.snk" $outDir "NServiceBus.Azure" $assemblies "dll"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
	
}

task compile_hosts  -depends init_enviorment {

	
	if(Test-Path "$build_base\hosting"){
	
		delete_directory "$build_base\hosting"
	}
	create_directory "$build_base\hosting"
	$solutions = dir "$base_dir\src\hosting\*.sln"
	$solutions | % {
		$solution_file = $_.FullName
		exec { &$script:msbuild $solution_file /p:OutDir="$build_base\hosting\" }		
	}
	
	
	$assemblies = @("$build_base\hosting\NServiceBus.Hosting.Windows.exe","$build_base\hosting\NServiceBus.Hosting.dll",
		"$build_base\hosting\Microsoft.Practices.ServiceLocation.dll","$build_base\hosting\Magnum.dll","$build_base\hosting\Topshelf.dll")
	
	echo "Merging NServiceBus.Host....."	
	Ilmerge "NServiceBus.snk" $outDir\host\ "NServiceBus.Host" $assemblies "exe"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
	

}

task compile_hosts32  -depends init_enviorment {

	
	
	$solutions = dir "$base_dir\src\hosting\*.sln"
	$solutions | % {
		$solution_file = $_.FullName
		
		exec { &$script:msbuild $solution_file /p:OutDir="$build_base\hosting32\" /t:Clean }
		
		exec { &$script:msbuild $solution_file /p:PlatformTarget=x86 /p:OutDir="$build_base\hosting32\"}
	}
	
	
	$assemblies = @("$build_base\hosting32\NServiceBus.Hosting.Windows.exe","$build_base\hosting32\NServiceBus.Hosting.dll",
		"$build_base\hosting32\Microsoft.Practices.ServiceLocation.dll","$build_base\hosting32\Magnum.dll","$build_base\hosting32\Topshelf.dll")
	
	echo "Merging NServiceBus.Host32....."	
	Ilmerge "NServiceBus.snk" $outDir\host\ "NServiceBus.Host32" $assemblies "exe"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
	

}

task compile_azure_hosts  -depends init_enviorment {

	$solutions = dir "$base_dir\src\azure\Hosting\*.sln"
	$solutions | % {
		$solution_file = $_.FullName
		exec { &$script:msbuild $solution_file /p:OutDir="$build_base\azure\Hosting\"}
	}
}

task test{
	
	if(Test-Path $build_base\test-reports){
		delete_directory $build_base\test-reports
	}
	
	create_directory $build_base\test-reports 
	
	$testAssemblies = @()
	$testAssemblies +=  dir $build_base\nservicebus.core\*Tests.dll -Exclude *FileShare.Tests.dll,*Gateway.Tests.dll, *Raven.Tests.dll, *Azure.Tests.dll 
	$testAssemblies +=  dir $build_base\containers\*Tests.dll -Exclude *FileShare.Tests.dll,*Gateway.Tests.dll, *Raven.Tests.dll, *Azure.Tests.dll 
	
	
	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework} 
}

task compile_tools -depends init_enviorment{
	$tools_dirs = "testing","claims","timeout","azure\timeout","proxy","tools\management\Errors\ReturnToSourceQueue\", "utils"
	
	$tools_dirs | % {				
	 	$solutions = dir "$base_dir\src\$_\*.sln"
		$currentOutDir = "$build_base\$_\"
		$solutions | % {
			$solution_file = $_.FullName
			exec { &$script:msbuild $solution_file /p:OutDir="$currentOutDir" }
		}
	}
	
	if(Test-Path $build_base\tools\MsmqUtils){
		delete_directory $build_base\tools\MsmqUtils
	}
	
	create_directory "$build_base\tools\MsmqUtils"
	Copy-Item $build_base\utils\*.* $build_base\tools\MsmqUtils -Force
	delete_directory $build_base\utils
	Copy-Item $build_base\tools\management\Errors\ReturnToSourceQueue\*.* $build_base\tools\ -Force
	
	cd $build_base\tools
	delete_directory "management"
	cd $base_dir
	
	
	
	exec {&$nunitexec "$build_base\testing\NServiceBus.Testing.Tests.dll" $script:nunitTargetFramework} 
		
	$assemblies = @("$build_base\testing\NServiceBus.Testing.dll", "$build_base\testing\Rhino.Mocks.dll");
	
	echo "Merging NServiceBus.Testing"	
	Ilmerge "NServiceBus.snk" $outDir\testing "NServiceBus.Testing" $assemblies "dll"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
	
	
	
	$assemblies = @("$build_base\nservicebus.core\XsdGenerator.exe",
	"$build_base\nservicebus.core\NServiceBus.Serializers.XML.dll", 
	"$build_base\nservicebus.core\NServiceBus.Utils.Reflection.dll")
	
	echo "merging XsdGenerator"	
	Ilmerge "NServiceBus.snk" $build_base\tools "XsdGenerator" $assemblies "exe"  $script:ilmergeTargetframework "/internalize:$base_dir\ilmerge.exclude"
}

task prepare_binaries -depends compile_main, compile_core, compile_containers, compile_webservices_integration, compile_nhibernate, compile_hosts, compile_hosts32, compile_azure, compile_azure_hosts, compile_tools {
	if(Test-Path $binaries_dir){
		delete_directory "binaries"
	}
	create_directory $binaries_dir;
	
	Copy-Item $outDir\NServiceBus*.* $binaries_dir -Force;
	Copy-Item $outDir\host\*.* $binaries_dir -Force;
	Copy-Item $outDir\testing\*.* $binaries_dir -Force;
	
	Copy-Item $libDir\log4net.dll $binaries_dir -Force;
	Copy-Item "$base_dir\packages\NUnit.2.5.10.11092\lib\nunit.framework.dll"  $binaries_dir -Force;
	Copy-Item "$libDir\sqlite\x86\*.dll"  $binaries_dir -Force;
	
	create_directory "$binaries_dir\x64"
	
	Copy-Item "$libDir\sqlite\x64\*.dll"  $binaries_dir\x64 -Force;
	
	create_directory "$binaries_dir\containers\autofac"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Autofac.dll"  $binaries_dir\containers\autofac -Force;
	
	create_directory "$binaries_dir\containers\castle"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.CastleWindsor.dll"  $binaries_dir\containers\castle -Force;
	
	create_directory "$binaries_dir\containers\structuremap"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.StructureMap.dll"  $binaries_dir\containers\structuremap -Force;
	
	create_directory "$binaries_dir\containers\spring"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Spring.dll"  $binaries_dir\containers\spring -Force;
			
	create_directory "$binaries_dir\containers\unity"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Unity.dll"  $binaries_dir\containers\unity -Force;		
		
	create_directory "$binaries_dir\containers\ninject"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Ninject.dll"  $binaries_dir\containers\ninject -Force;	
}

task compile_samples -depends init_enviorment, prepare_binaries {

	$samples_dirs = "AsyncPages","FullDuplex","PubSub","Manufacturing","GenericHost","Versioning","WcfIntegration","Starbucks","SendOnlyEndpoint","DataBus","Azure\AzureBlobStorageDataBus","Distributor"
	
	$samples_dirs | % {				
	 	$solutions = dir "$base_dir\Samples\$_\*.sln"
		$solutions | % {
			$solution_file = $_.FullName
			exec {&$script:msbuild $solution_file}
		}
	}

}

task prepare_release -depends prepare_binaries, test, compile_samples {
	
	if(Test-Path $release_root){
		delete_directory $release_root	
	}
	
	create_directory $release_root
	if ($targetframework -eq "net-4.0"){
		$release_dir = "$release_root\net40"
	}
	create_directory $release_dir

	 
	Copy-Item -Force "$base_dir\*.txt" $release_root  -ErrorAction SilentlyContinue
	Copy-Item -Force "$base_dir\RunMeFirst.bat" $release_root -ErrorAction  SilentlyContinue
	
	Copy-Item -Force -Recurse "$build_base\tools" $release_root\tools -ErrorAction SilentlyContinue
	
	cd $release_root\tools
	dir -recurse -include ('*.xml', '*.pdb') |ForEach-Object {
	write-host deleting $_ 
	Remove-Item $_ 
	}
	cd $base_dir
	
	Copy-Item -Force -Recurse "$base_dir\docs" $release_root\docs -ErrorAction SilentlyContinue
	
	Copy-Item -Force -Recurse "$base_dir\Samples" $release_dir\samples  -ErrorAction SilentlyContinue 
	cd $release_dir\samples 
	
	dir -recurse -include ('bin', 'obj') |ForEach-Object {
	write-host deleting $_ 
	delete_directory $_
	}
	
	cd $base_dir
	
	
	
	create_directory "$release_dir\processes"
	$processes_dir = "$release_dir\processes" 
	
	Copy-Item -Force -Recurse "$build_base\timeout" $processes_dir\timeout -ErrorAction SilentlyContinue
	cd $processes_dir\timeout
	dir -recurse -include ('*.xml', '*.pdb') |ForEach-Object {
	write-host deleting $_ 
	Remove-Item $_ 
	}
	cd $base_dir
	
	Copy-Item -Force -Recurse "$build_base\proxy" $processes_dir\proxy -ErrorAction SilentlyContinue
	cd $processes_dir\proxy
	dir -recurse -include ('*.xml', '*.pdb') |ForEach-Object {
	write-host deleting $_ 
	Remove-Item $_ 
	}
	cd $base_dir
	
	Copy-Item -Force -Recurse "$base_dir\binaries" $release_dir\binaries -ErrorAction SilentlyContinue  
	

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
task detect_operating_system_architecture {
	echo $script:architecture
	if (IsWow64 -eq $true)
	{
		$script:architecture = "x64"
	}
    echo $script:architecture
}
  
task geneate_common_assembly_info -depends install_dependent_packages {
	$buildNumber = 0
	if($env:BUILD_NUMBER -ne $null) {
    	$buildNumber = $env:BUILD_NUMBER
	}
	Write-Output "Build Number: $buildNumber"
	
	$fileVersion = $productVersion + "." + $patchVersion + "." + $buildNumber 
	$asmVersion =  $productVersion + ".0.0"
	$infoVersion = $asmVersion+$preRelease+$buildNumber 
	#later after Nuget 1.6 release $script:packageVersion = $infoVersion;
	$script:packageVersion = $fileVersion;
	
	
	Generate-Assembly-Info true "release" "The most popular open-source service bus for .net" "NServiceBus" "NServiceBus" "Copyright � NServiceBus 2007-2011" $asmVersion $fileVersion $infoVersion "$base_dir\src\CommonAssemblyInfo.cs" 
	
	if($env:BUILD_NUMBER -ne $null) {
		$env:BUILD_NUMBER = $infoVersion
	}
 }
 
task install_dependent_packages {
 	dir -recurse -include ('packages.config') |ForEach-Object {
	$packageconfig = [io.path]::Combine($_.directory,$_.name)

	write-host $packageconfig 

	 exec{ &$nugetExec install $packageconfig -o packages } 
	}
 }

task prepare_and_release_nservicebus -depends prepare_release, create_packages, zip_output{
    if(Test-Path -Path $release_dir)
	{
        del -Path $release_dir -Force -recurse
	}	
	echo Released $script:releaseVersion 
}

task zip_output {

	
	echo "Cleaning the Release Artifacts before ziping"
	
	$packagingArtifacts = "$release_dir\PackagingArtifacts"
	$packageOutPutDir = "$release_dir\packages"
	
	if(Test-Path -Path $packagingArtifacts ){
		delete_directory $packagingArtifacts
	}
	
	if((Test-Path -Path $packageOutPutDir) -and ($uploadPackage) ){
        delete_directory $packageOutPutDir
	}

	echo "Zip Output"	
	$buildNumber = 0
	if($env:BUILD_NUMBER -ne $null) {
    	$buildNumber = $env:BUILD_NUMBER
	}
	
	$script:releaseVersion = $buildNumber
	
	if((Test-Path -Path $artifacts_dir) -eq $true)
	{
		delete_directory $artifacts_dir
	}
	
    create_directory $artifacts_dir
	
	$archive = "$artifacts_dir\NServiceBus.$script:releaseVersion.zip"
	exec { &$zipExec a -tzip $archive $release_dir\** }

    echo "Zip Output Over"

}

task update_packages{
	dir -recurse -include ('packages.config') |ForEach-Object {
		$packageconfig = [io.path]::Combine($_.directory,$_.name)

		write-host $packageconfig

		if($packageIds -ne "")
		{
			write-host "Doing an unsafe update of" $packageIds 
			exec{&$nugetExec update $packageconfig -RepositoryPath packages -Id $packageIds}
		}
		else
		{	
			write-host "Doing a safe update of all packages" $packageIds 
			exec{&$nugetExec update -Safe $packageconfig -RepositoryPath packages}
		}
	}
}

function delete_directory($directory_name){
	Remove-Item -Force -Recurse $directory_name -ErrorAction SilentlyContinue
}
 
function create_directory($directory_name){
	New-Item $directory_name -ItemType Directory | Out-Null
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