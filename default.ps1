properties {
	$ProductVersion = "3.2"
	$BuildNumber = "0";
	$PatchVersion = "0"
	$PreRelease = "-build"	
	$PackageNameSuffix = ""
	$TargetFramework = "net-4.0"
	$UploadPackage = $false;
	$NugetKey = ""
	$PackageIds = ""
	$DownloadDependentPackages = $false
	$buildConfiguration = "Debug"
	
}

$baseDir  = resolve-path .
$releaseRoot = "$baseDir\Release"
$releaseDir = "$releaseRoot\net40"
$binariesDir = "$baseDir\binaries"
$coreOnlyDir = "$baseDir\core-only"
$srcDir = "$baseDir\src"
$coreOnlyBinariesDir = "$coreOnlyDir\binaries"
$buildBase = "$baseDir\build"
$outDir =  "$buildBase\output"
$coreOnly =  "$buildBase\coreonly"
$libDir = "$baseDir\lib" 
$artifactsDir = "$baseDir\artifacts"
$toolsDir = "$baseDir\tools"
$nunitexec = "packages\NUnit.2.5.10.11092\tools\nunit-console.exe"
$nugetExec = "$toolsDir\NuGet\NuGet.exe"
$zipExec = "$toolsDir\zip\7za.exe"
$ilMergeKey = "$srcDir\NServiceBus.snk"
$ilMergeExclude = "$toolsDir\IlMerge\ilmerge.exclude"
$script:architecture = "x86"
$script:ilmergeTargetFramework = ""
$script:msBuildTargetFramework = ""	
$script:nunitTargetFramework = "/framework=4.0";
$script:msBuild = ""
$script:isEnvironmentInitialized = $false
$script:packageVersion = "3.2.0-local"
$script:releaseVersion = ""

include $toolsDir\psake\buildutils.ps1

task default -depends ReleaseNServiceBus -description "Invokes ReleaseNServiceBus task"
 
task Clean -description "Cleans the eviorment for the build" {

	if(Test-Path $buildBase){
		Delete-Directory $buildBase
		
	}
	
	if(Test-Path $artifactsDir){
		Delete-Directory $artifactsDir
		
	}
	
	if(Test-Path $binariesDir){
		Delete-Directory $binariesDir
		
	}
	
	if(Test-Path $coreOnlyDir){
		Delete-Directory $coreOnlyDir
		
	}
}

task InitEnvironment -description "Initializes the environment for build" {

	if($script:isEnvironmentInitialized -ne $true){
		if ($TargetFramework -eq "net-4.0"){
			$netfxInstallroot ="" 
			$netfxInstallroot =	Get-RegistryValue 'HKLM:\SOFTWARE\Microsoft\.NETFramework\' 'InstallRoot' 
			
			$netfxCurrent = $netfxInstallroot + "v4.0.30319"
			
			$script:msBuild = $netfxCurrent + "\msbuild.exe"
			
			echo ".Net 4.0 build requested - $script:msBuild" 

			$script:ilmergeTargetFramework  = "/targetplatform:v4," + $netfxCurrent
			
			$script:msBuildTargetFramework ="/p:TargetFrameworkVersion=v4.0 /ToolsVersion:4.0"
			
			$script:nunitTargetFramework = "/framework=4.0";
			
			$script:isEnvironmentInitialized = $true
		}
	}
	$binariesExists = Test-Path $binariesDir;
	if($binariesExists -eq $false){	
		Create-Directory $binariesDir
		echo "created binaries"
	}
}

task Init -depends Clean, InstallDependentPackages, DetectOperatingSystemArchitecture -description "Initializes the build" {
   	
	echo "Creating build directory at the following path $buildBase"
	Delete-Directory $buildBase
	Create-Directory $buildBase
	
	$currentDirectory = Resolve-Path .
	
	echo "Current Directory: $currentDirectory" 
 }
  
task CompileMain -depends InitEnvironment -description "Builds NServiceBus.dll and keeps the output in \binaries" { 

	$solutions = dir "$srcDir\core\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\nservicebus\" /p:Configuration=$buildConfiguration }
	}
	
	$assemblies = @()
	$assemblies +=	dir $buildBase\nservicebus\NServiceBus.dll
	$assemblies  +=  dir $buildBase\nservicebus\NServiceBus*.dll -Exclude NServiceBus.dll, **Tests.dll

	Ilmerge $ilMergeKey $outDir "NServiceBus" $assemblies "" "dll" $script:ilmergeTargetFramework "$buildBase\NServiceBusMergeLog.txt" $ilMergeExclude
	
	Copy-Item $outDir\NServiceBus.dll $binariesDir -Force;
	Copy-Item $outDir\NServiceBus.pdb $binariesDir -Force;
	Copy-Item $libDir\log4net.dll $binariesDir -Force;

}

task TestMain -depends CompileMain -description "Builds NServiceBus.dll, keeps the output in \binaries and unit tests the code responsible for NServiceBus.dll"{

	if((Test-Path -Path $buildBase\test-reports) -eq $false){
		Create-Directory $buildBase\test-reports 
	}
	$testAssemblies = @()
	$testAssemblies +=  dir $buildBase\nservicebus\*Tests.dll -Exclude *FileShare.Tests.dll,*Gateway.Tests.dll, *Raven.Tests.dll, *Azure.Tests.dll 
	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework}
}

task CompileCore -depends InitEnvironment -description "Builds NServiceBus.Core.dll and keeps the output in \binaries" { 

$coreDirs = "unicastTransport", "ObjectBuilder", "config", "faults", "utils", "messageInterfaces", "impl\messageInterfaces", "config", "logging",  "Impl\ObjectBuilder.Common", "installation", "encryption", "unitofwork", "masterNode", "impl\installation", "impl\unicast\NServiceBus.Unicast.Msmq", "impl\Serializers", "impl\licensing", "unicast", "headers", "impersonation", "impl\unicast\queuing", "impl\unicast\transport", "impl\unicast\NServiceBus.Unicast.Subscriptions.Msmq", "impl\unicast\NServiceBus.Unicast.Subscriptions.InMemory", "impl\faults", "impl\encryption", "databus", "impl\Sagas", "impl\SagaPersisters\InMemory", "impl\SagaPersisters\RavenSagaPersister", "impl\unicast\NServiceBus.Unicast.Subscriptions.Raven", "integration", "impl\databus", "distributor", "gateway", "scheduling", "satellites", "management\retries", "timeout"
	$coreDirs | % {
		$solutionDir = Resolve-Path "$srcDir\$_"
		cd 	$solutionDir
	 	$solutions = dir "*.sln"
		$solutions | % {
			$solutionFile = $_.FullName
			exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\nservicebus.core\" /p:Configuration=$buildConfiguration }
		}
	}
	cd $baseDir
	
	$solutions = dir "$srcDir\AttributeAssemblies\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\attributeAssemblies\" /p:Configuration=$buildConfiguration }
	}
	
	$attributeAssembly = "$buildBase\attributeAssemblies\NServiceBus.Core.dll"
	
	$assemblies  =  dir $buildBase\nservicebus.core\NServiceBus.**.dll -Exclude **Tests.dll 
	Ilmerge $ilMergeKey $coreOnly "NServiceBus.Core" $assemblies $attributeAssembly "dll" $script:ilmergeTargetFramework "$buildBase\NServiceBusCoreCore-OnlyMergeLog.txt" $ilMergeExclude
	
	<#It's Possible to copy the NServiceBus.Core.dll to Core-Only but not done gain time on development build #>
			
	$assemblies += dir $buildBase\nservicebus.core\antlr3*.dll
	$assemblies += dir $buildBase\nservicebus.core\common.logging.dll
	$assemblies += dir $buildBase\nservicebus.core\common.logging.log4net.dll
	$assemblies += dir $buildBase\nservicebus.core\Interop.MSMQ.dll
	$assemblies += dir $buildBase\nservicebus.core\AutoFac.dll
	$assemblies += dir $buildBase\nservicebus.core\NLog.dll
	$assemblies += dir $buildBase\nservicebus.core\Raven.Abstractions.dll
	$assemblies += dir $buildBase\nservicebus.core\Raven.Client.Lightweight.dll
	$assemblies += dir $buildBase\nservicebus.core\rhino.licensing.dll
	$assemblies += dir $buildBase\nservicebus.core\Newtonsoft.Json.dll

	Ilmerge $ilMergeKey $outDir "NServiceBus.Core" $assemblies $attributeAssembly "dll"  $script:ilmergeTargetFramework "$buildBase\NServiceBusCoreMergeLog.txt"  $ilMergeExclude
	
	Copy-Item $outDir\NServiceBus.Core.dll $binariesDir -Force;
	Copy-Item $outDir\NServiceBus.Core.pdb $binariesDir -Force;
	
}

task TestCore  -depends CompileCore -description "Builds NServiceBus.Core.dll, keeps the output in \binaries and unit tests the code responsible for NServiceBus.Core.dll" {
	
	if((Test-Path -Path $buildBase\test-reports) -eq $false){
		Create-Directory $buildBase\test-reports 
	}	
	$testAssemblies = @()
	$testAssemblies +=  dir $buildBase\nservicebus.core\*Tests.dll -Exclude *FileShare.Tests.dll,*Gateway.Tests.dll, *Raven.Tests.dll, *Azure.Tests.dll 
	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework}
}

task CompileContainers -depends InitEnvironment -description "Builds the container dlls for autofac, castle, ninject, spring, structuremap and MS unity and keeps the output in respective folders in binaries\containers" {

	$solutions = dir "$srcDir\impl\ObjectBuilder\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\containers\" /p:Configuration=$buildConfiguration }		
	}
	
	if(Test-Path "$buildBase\output\containers"){
		Delete-Directory "$buildBase\output\containers"
	}	
	Create-Directory "$buildBase\output\containers"
	Copy-Item $buildBase\containers\NServiceBus.ObjectBuilder.**.* $buildBase\output\containers -Force
	
	if(Test-Path "$coreOnly\containers"){
		Delete-Directory "$coreOnly\containers"
	}
	
	if(Test-Path "$binariesDir\containers"){
		Delete-Directory "$binariesDir\containers"
	}
	
	Create-Directory "$binariesDir\containers\autofac"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Autofac.*"  $binariesDir\containers\autofac -Force -Exclude *.pdb
		
	Create-Directory "$binariesDir\containers\castle"	
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.CastleWindsor.*"  $binariesDir\containers\castle -Force -Exclude *.pdb
	
	
	Create-Directory "$binariesDir\containers\structuremap"	
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.StructureMap.*"  $binariesDir\containers\structuremap -Force -Exclude *.pdb
	
	
	Create-Directory "$binariesDir\containers\spring"	
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Spring.*"  $binariesDir\containers\spring -Force -Exclude *.pdb
	
			
	Create-Directory "$binariesDir\containers\unity"	
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Unity.*"  $binariesDir\containers\unity -Force -Exclude *.pdb
	
		
	Create-Directory "$binariesDir\containers\ninject"	
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Ninject.*"  $binariesDir\containers\ninject -Force -Exclude *.pdb	
	
	
}

task TestContainers  -depends CompileContainers -description "Builds the container dlls for autofac, castle, ninject, spring, structuremap and MS unity, keeps the output in respective folders in binaries\containers and unit tests the code responsible for each container assemblies"  {
	
	if((Test-Path -Path $buildBase\test-reports) -eq $false){
		Create-Directory $buildBase\test-reports 
	}	
	$testAssemblies = @()
	$testAssemblies +=  dir $buildBase\containers\*Tests.dll -Exclude *FileShare.Tests.dll,*Gateway.Tests.dll, *Raven.Tests.dll, *Azure.Tests.dll 
	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework}
}

task CompileWebServicesIntegration -depends InitEnvironment -description "Builds NServiceBus.Integration.WebServices.dll and keeps the output in \binaries"{

	$solutions = dir "$srcDir\integration\WebServices\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$outDir\" /p:Configuration=$buildConfiguration }		
	}
	
	Copy-Item $outDir\NServiceBus.Integration.*.* $binariesDir -Force;
}

task CompileNHibernate -depends InitEnvironment -description "Builds NServiceBus.NHibernate.dll and keeps the output in \binaries" {

	$solutions = dir "$srcDir\nhibernate\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\NServiceBus.NHibernate\" /p:Configuration=$buildConfiguration }		
	}
	$assemblies = dir $buildBase\NServiceBus.NHibernate\NServiceBus.**NHibernate**.dll -Exclude **Tests.dll
	Ilmerge  $ilMergeKey $outDir "NServiceBus.NHibernate" $assemblies "" "dll"  $script:ilmergeTargetFramework "$buildBase\NServiceBusNHibernateMergeLog.txt"  $ilMergeExclude
	
	Copy-Item $outDir\NServiceBus.NHibernate.dll $binariesDir -Force;
	Copy-Item $outDir\NServiceBus.NHibernate.pdb $binariesDir -Force;
}

task TestNHibernate  -depends CompileNHibernate -description "Builds NServiceBus.NHibernate.dll, keeps the output in \binaries  and unit tests the code responsible for NServiceBus.NHibernate.dll"  {

	if((Test-Path -Path $buildBase\test-reports) -eq $false){
		Create-Directory $buildBase\test-reports 
	}	
	$testAssemblies = @()
	$testAssemblies +=  dir $buildBase\NServiceBus.NHibernate\**Tests.dll -Exclude *FileShare.Tests.dll,*Gateway.Tests.dll, *Raven.Tests.dll, *Azure.Tests.dll 
	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework}
}

task CompileAzure -depends InitEnvironment -description "Builds NServiceBus.Azure.dll and keeps the output in \binaries"   {

	$solutions = dir "$srcDir\azure\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\azure\NServiceBus.Azure\" /p:Configuration=$buildConfiguration }		
	}
	$attributeAssembly = "$buildBase\attributeAssemblies\NServiceBus.Azure.dll"
	$assemblies = dir $buildBase\azure\NServiceBus.Azure\NServiceBus.**Azure**.dll -Exclude **Tests.dll
	$assemblies += dir $buildBase\azure\NServiceBus.Azure\NServiceBus.**AppFabric**.dll -Exclude **Tests.dll
	
	Ilmerge $ilMergeKey $outDir "NServiceBus.Azure" $assemblies $attributeAssembly "dll" $script:ilmergeTargetFramework "$buildBase\NServiceBusAzureMergeLog.txt"  $ilMergeExclude
	
	Copy-Item $outDir\NServiceBus.Azure.dll $binariesDir -Force;
	Copy-Item $outDir\NServiceBus.Azure.pdb $binariesDir -Force;
	
}

task TestAzure  -depends CompileAzure -description "Builds NServiceBus.Azure.dll, keeps the output in \binaries and unit test the code responsible for NServiceBus.Azure.dll" {

	if((Test-Path -Path $buildBase\test-reports) -eq $false){
		Create-Directory $buildBase\test-reports 
	}	
	$testAssemblies = @()
	$testAssemblies +=  dir $buildBase\azure\NServiceBus.Azure\**Tests.dll -Exclude *FileShare.Tests.dll,*Gateway.Tests.dll, *Raven.Tests.dll
	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework}
}

task CompileHosts  -depends InitEnvironment -description "Builds NServiceBus.Host.exe and keeps the output in \binaries"  {

	if(Test-Path "$buildBase\hosting"){
	
		Delete-Directory "$buildBase\hosting"
	}
	Create-Directory "$buildBase\hosting"
	$solutions = dir "$srcDir\hosting\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\hosting\" /p:Configuration=$buildConfiguration }		
	}
	
	$assemblies = @("$buildBase\hosting\NServiceBus.Hosting.Windows.exe", "$buildBase\hosting\NServiceBus.Hosting.dll",
		"$buildBase\hosting\Microsoft.Practices.ServiceLocation.dll", "$buildBase\hosting\Magnum.dll", "$buildBase\hosting\Topshelf.dll")
	
	Ilmerge $ilMergeKey $outDir\host\ "NServiceBus.Host" $assemblies "" "exe"  $script:ilmergeTargetFramework "$buildBase\NServiceBusHostMergeLog.txt"  $ilMergeExclude
	
	Copy-Item $outDir\host\NServiceBus.Host.exe $binariesDir -Force;
	Copy-Item $outDir\host\NServiceBus.Host.pdb $binariesDir -Force;
}

task CompileHosts32  -depends InitEnvironment -description "Builds NServiceBus.Host32.exe and keeps the output in \binaries" {		
	$solutions = dir "$srcDir\hosting\*.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\hosting32\" /t:Clean }
		
		exec { &$script:msBuild $solutionFile /p:PlatformTarget=x86 /p:OutDir="$buildBase\hosting32\" /p:Configuration=$buildConfiguration}
	}
	
	
	$assemblies = @("$buildBase\hosting32\NServiceBus.Hosting.Windows.exe", "$buildBase\hosting32\NServiceBus.Hosting.dll",
		"$buildBase\hosting32\Microsoft.Practices.ServiceLocation.dll", "$buildBase\hosting32\Magnum.dll", "$buildBase\hosting32\Topshelf.dll")
	
	Ilmerge $ilMergeKey $outDir\host\ "NServiceBus.Host32" $assemblies "" "exe"  $script:ilmergeTargetFramework "$buildBase\NServiceBusHostMerge32Log.txt"  $ilMergeExclude
	
	Copy-Item $outDir\host\NServiceBus.Host32.exe $binariesDir -Force;
	Copy-Item $outDir\host\NServiceBus.Host32.pdb $binariesDir -Force;
}

task CompileAzureHosts  -depends InitEnvironment -description "Builds NServiceBus.Hosting.Azure.dll and NServiceBus.Hosting.Azure.HostProcess.exe and keeps the outputs in \binaries" {

	$solutions = dir "$srcDir\azure\Hosting\NServiceBus.Hosting.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\azure\Hosting\" /p:Configuration=$buildConfiguration}
	}
	
	$assemblies = @("$buildBase\azure\Hosting\NServiceBus.Hosting.Azure.dll",
		"$buildBase\azure\Hosting\NServiceBus.Hosting.dll")
	
	Ilmerge $ilMergeKey $outDir "NServiceBus.Hosting.Azure" $assemblies "" "dll"  $script:ilmergeTargetFramework "$buildBase\NServiceBusAzureHostMergeLog.txt"  $ilMergeExclude
	
	Copy-Item $outDir\NServiceBus.Hosting.Azure.dll $binariesDir -Force;
	Copy-Item $outDir\NServiceBus.Hosting.Azure.pdb $binariesDir -Force;
	
	$solutions = dir "$srcDir\azure\Timeout\Timeout.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\azure\Timeout\" /p:Configuration=$buildConfiguration}
	}
	
	echo "Copying NServiceBus.Timeout.Hosting.Azure....."	
	Copy-Item $buildBase\azure\Timeout\NServiceBus.Timeout.Hosting.Azure.* $buildBase\output -Force
	
	$solutions = dir "$srcDir\azure\Hosting\NServiceBus.Hosting.HostProcess.sln"
	$solutions | % {
		$solutionFile = $_.FullName
		exec { &$script:msBuild $solutionFile /p:OutDir="$buildBase\azure\Hosting\" /p:Configuration=$buildConfiguration}
	}
	
	$assemblies = @("$buildBase\azure\Hosting\NServiceBus.Hosting.Azure.HostProcess.exe",
		"$buildBase\azure\Hosting\Magnum.dll", "$buildBase\azure\Hosting\Topshelf.dll")
	
	Ilmerge $ilMergeKey $outDir\host\ "NServiceBus.Hosting.Azure.HostProcess" $assemblies "" "exe"  $script:ilmergeTargetFramework "$buildBase\NServiceBusAzureHostProcessMergeLog.txt"  $ilMergeExclude
	
	Copy-Item $outDir\host\NServiceBus.Hosting.Azure.HostProcess.exe $binariesDir -Force;
	Copy-Item $outDir\host\NServiceBus.Hosting.Azure.HostProcess.pdb $binariesDir -Force;
}

task CompileTools -depends InitEnvironment -description "Builds the tools XsdGenerator.exe, runner.exe and ReturnToSourceQueue.exe." {
	$toolsDirs = "testing", "claims", "timeout", "proxy", "tools\management\Errors\ReturnToSourceQueue\", "utils","tools\migration\"
	
	$toolsDirs | % {				
	 	$solutions = dir "$srcDir\$_\*.sln"
		$currentOutDir = "$buildBase\$_\"
		$solutions | % {
			$solutionFile = $_.FullName
			exec { &$script:msBuild $solutionFile /p:OutDir="$currentOutDir" /p:Configuration=$buildConfiguration}
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
	
	Create-Directory $outDir\testing
	Copy-Item $buildBase\testing\NServiceBus.Testing.dll $outDir\testing -Force;
	Copy-Item $buildBase\testing\NServiceBus.Testing.pdb $outDir\testing -Force;
	
	Copy-Item $outDir\testing\*.* $binariesDir -Force;
	
	$assemblies = @("$buildBase\nservicebus.core\XsdGenerator.exe",
	"$buildBase\nservicebus.core\NServiceBus.Serializers.XML.dll", 
	"$buildBase\nservicebus.core\NServiceBus.Utils.Reflection.dll")
	
	Ilmerge $ilMergeKey $buildBase\tools "XsdGenerator" $assemblies "" "exe" $script:ilmergeTargetFramework "$buildBase\XsdGeneratorMergeLog.txt"  $ilMergeExclude
}

task TestTools -depends CompileTools -description "Builds the tools XsdGenerator.exe, runner.exe and ReturnToSourceQueue.exe and unit tests the code responsible for the tools XsdGenerator.exe, runner.exe and ReturnToSourceQueue.exe " {
	exec {&$nunitexec "$buildBase\testing\NServiceBus.Testing.Tests.dll" $script:nunitTargetFramework} 
}

task Test -depends TestMain, TestCore, TestContainers, TestNHibernate, TestTools <#, TestAzure #> -description "Builds and unit tests all the source which has unit tests"  {	
}

task PrepareBinaries -depends Init, CompileMain, CompileCore, CompileContainers, CompileWebServicesIntegration, CompileNHibernate, CompileHosts, CompileHosts32, CompileAzure, CompileAzureHosts, CompileTools, Test   -description "Builds all the source code in order, Runs thet unit tests and prepares the binaries and Core-only binaries" {
	Prepare-Binaries
}

task JustPrepareBinaries -depends Init, CompileMain, CompileCore, CompileContainers, CompileWebServicesIntegration, CompileNHibernate, CompileHosts, CompileHosts32, CompileAzure, CompileAzureHosts -description "Builds All the source code, just prepare the binaries without testing it" {
}

function Prepare-Binaries{
	if(Test-Path $binariesDir){
		Delete-Directory "binaries"
	}
	
	Create-Directory $binariesDir
	
	Create-Directory $coreOnlyDir
	Create-Directory $coreOnlyBinariesDir
	
	Copy-Item $outDir\NServiceBus*.* $binariesDir -Force;
	
	Copy-Item $outDir\NServiceBus.dll $coreOnlyBinariesDir -Force;
	Copy-Item $outDir\NServiceBus.NHibernate.dll $coreOnlyBinariesDir -Force;
	Copy-Item $outDir\NServiceBus.Azure.dll $coreOnlyBinariesDir -Force;
	Copy-Item $coreOnly\NServiceBus*.* $coreOnlyBinariesDir -Force;
	
	Copy-Item $outDir\host\*.* $binariesDir -Force;
	Copy-Item $outDir\host\*.* $coreOnlyBinariesDir -Force;
	
	Copy-Item $outDir\testing\*.* $binariesDir -Force;
	Copy-Item $outDir\testing\*.* $coreOnlyBinariesDir -Force;
	
	Copy-Item $libDir\log4net.dll $binariesDir -Force;
	
	
	Create-Directory "$binariesDir\containers\autofac"
	Create-Directory "$coreOnlyBinariesDir\containers\autofac"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Autofac.*"  $binariesDir\containers\autofac -Force -Exclude *.pdb
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Autofac.*"  $coreOnlyBinariesDir\containers\autofac -Force -Exclude *.pdb
	
	Create-Directory "$binariesDir\containers\castle"
	Create-Directory "$coreOnlyBinariesDir\containers\castle"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.CastleWindsor.*"  $binariesDir\containers\castle -Force -Exclude *.pdb
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.CastleWindsor.*"  $coreOnlyBinariesDir\containers\castle -Force -Exclude *.pdb
	
	Create-Directory "$binariesDir\containers\structuremap"
	Create-Directory "$coreOnlyBinariesDir\containers\structuremap"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.StructureMap.*"  $binariesDir\containers\structuremap -Force -Exclude *.pdb
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.StructureMap.*"  $coreOnlyBinariesDir\containers\structuremap -Force -Exclude *.pdb
	
	Create-Directory "$binariesDir\containers\spring"
	Create-Directory "$coreOnlyBinariesDir\containers\spring"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Spring.*"  $binariesDir\containers\spring -Force -Exclude *.pdb
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Spring.*"  $coreOnlyBinariesDir\containers\spring -Force -Exclude *.pdb
			
	Create-Directory "$binariesDir\containers\unity"
	Create-Directory "$coreOnlyBinariesDir\containers\unity"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Unity.*"  $binariesDir\containers\unity -Force -Exclude *.pdb
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Unity.*"  $coreOnlyBinariesDir\containers\unity -Force -Exclude *.pdb		
		
	Create-Directory "$binariesDir\containers\ninject"
	Create-Directory "$coreOnlyBinariesDir\containers\ninject"
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Ninject.*"  $binariesDir\containers\ninject -Force -Exclude *.pdb	
	Copy-Item "$outDir\containers\NServiceBus.ObjectBuilder.Ninject.*"  $coreOnlyBinariesDir\containers\ninject -Force -Exclude *.pdb	
	
	Create-Directory $coreOnlyDir\dependencies\
	Copy-Item $buildBase\nservicebus.core\antlr3*.dll $coreOnlyDir\dependencies\	-Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\common.logging.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\common.logging.log4net.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\Interop.MSMQ.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\AutoFac.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\Raven*.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll, Raven.Client.Debug.dll, Raven.Client.MvcIntegration.dll
	Copy-Item $buildBase\nservicebus.core\NLog.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\rhino.licensing.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\Newtonsoft.Json.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\ICSharpCode.NRefactory.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\Esent.Interop.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\Lucene.Net.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\Lucene.Net.Contrib.SpellChecker.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\Lucene.Net.Contrib.Spatial.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
	Copy-Item $buildBase\nservicebus.core\BouncyCastle.Crypto.dll $coreOnlyDir\dependencies\ -Exclude **Tests.dll
}

task PrepareBinariesWithGeneratedAssemblyIno -depends GenerateAssemblyInfo, PrepareBinaries -description "Builds all the source code except samples in order, runs the unit tests and prepare the binaries and Core-only binaries" {}

task CompileSamples -depends InitEnvironment -description "Compiles all the sample projects." {
	$excludeFromBuild = @("AsyncPagesMVC3.sln", "AzureFullDuplex.sln", "AzureHost.sln", "AzurePubSub.sln", "AzureThumbnailCreator.sln", 
						  "ServiceBusFullDuplex.sln", "AzureServiceBusPubSub.sln")
	$solutions = ls -path $baseDir\Samples -include *.sln -recurse  
		$solutions | % {
			$solutionName =  [System.IO.Path]::GetFileName($_.FullName)
				if([System.Array]::IndexOf($excludeFromBuild, $solutionName) -eq -1){
				$solutionFile = $_.FullName
				exec {&$script:msBuild $solutionFile}
			}
		}
	}

task CompileSamplesFull -depends InitEnvironment, PrepareBinaries, CompileSamples -description "Compiles all the sample projects after compiling the full Sourc in order." {}  

task PrepareRelease -depends GenerateAssemblyInfo, PrepareBinaries, CompileSamples -description "Compiles all the source code in order, runs the unit tests, prepare the binaries and Core-only binaries, compiles all the sample projects and prepares for the release artifacts" {
	
	if(Test-Path $releaseRoot){
		Delete-Directory $releaseRoot	
	}
	
	Create-Directory $releaseRoot
	if ($TargetFramework -eq "net-4.0"){
		$releaseDir = "$releaseRoot\net40"
	}
	Create-Directory $releaseDir

	 
	Copy-Item -Force "$baseDir\*.txt" $releaseRoot  -ErrorAction SilentlyContinue
	Copy-Item -Force "$baseDir\*.txt" $coreOnlyDir  -ErrorAction SilentlyContinue
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
	Copy-Item -Force -Recurse "$baseDir\docs" $coreOnlyDir\docs -ErrorAction SilentlyContinue
	
	Copy-Item -Force -Recurse "$baseDir\Samples" $releaseRoot\samples  -ErrorAction SilentlyContinue 
	cd $releaseRoot\samples 
	
	dir -recurse -include ('bin', 'obj', 'packages') |ForEach-Object {
	write-host deleting $_ 
	Delete-Directory $_
	}
	cd $baseDir
	
	Copy-Item -Force -Recurse "$baseDir\binaries" $releaseDir\binaries -ErrorAction SilentlyContinue  
}

task CreatePackages -depends PrepareRelease  -description "After preparing for Release creates the nuget packages and if UploadPackage is set to true then publishes the packages to Nuget gallery "  {

	if(($UploadPackage) -and ($NugetKey -eq "")){
		throw "Could not find the NuGet access key Package Cannot be uploaded without access key"
	}
		
	import-module $toolsDir\NuGet\packit.psm1
	Write-Output "Loading the module for packing.............."
	$packit.push_to_nuget = $UploadPackage 
	$packit.nugetKey  = $NugetKey
	
	$packit.framework_Isolated_Binaries_Loc = "$baseDir\release"
	$packit.PackagingArtifactsRoot = "$baseDir\release\PackagingArtifacts"
	$packit.packageOutPutDir = "$baseDir\release\packages"

	$packit.targeted_Frameworks = "net40";


	#region Packing NServiceBus
	$packageNameNsb = "NServiceBus" + $PackageNameSuffix 	
	$packit.package_description = "The most popular open-source service bus for .net"
	invoke-packit $packageNameNsb $script:packageVersion @{log4net="[1.2.10]"} "binaries\NServiceBus.dll", "binaries\NServiceBus.Core.dll", "binaries\NServiceBus.xml", "binaries\NServiceBus.Core.xml" @{} 
	#endregion
	
	#region Packing NServiceBus.Interfaces
	$packageName = "NServiceBus.Interfaces" + $PackageNameSuffix 	
	$packit.package_description = "The Interfaces for NServiceBus Implementation"
	invoke-packit $packageName $script:packageVersion @{} "binaries\NServiceBus.dll", "binaries\NServiceBus.xml"  @{} 
	#endregion
	
    #region Packing NServiceBus.Host
	
	$appConfigTranformContent = "<?xml version=`"1.0`"?>
<configuration>
  <configSections>
    <section name=`"MessageForwardingInCaseOfFaultConfig`" type=`"NServiceBus.Config.MessageForwardingInCaseOfFaultConfig, NServiceBus.Core`" />
   </configSections>
  <MessageForwardingInCaseOfFaultConfig ErrorQueue=`"error`"/>
</configuration>
"
    $installPs1Content = "param(`$installPath, `$toolsPath, `$package, `$project)
	
	`$directoryName  = [system.io.Path]::GetDirectoryName(`$project.FullName)	
	`$appConfigFile = `$directoryName + `"\App.config`"
	if((Test-Path -Path `$appConfigFile) -eq `$true){
		[xml] `$appConfig = Get-Content `$appConfigFile
		`$selectedNodes = Select-Xml -XPath `"/configuration/MessageForwardingInCaseOfFaultConfig`" -Xml `$appConfig
		if(`$selectedNodes -ne `$null){
			`$selectedNodes.Count
			if(`$selectedNodes.Count -gt 1){
				`$selectedNode = Select-Xml -XPath `"/configuration/MessageForwardingInCaseOfFaultConfig[@ErrorQueue='error' ]`" -Xml `$appConfig
				`$appConfig | select-xml -xpath `"/configuration`" | % {`$_.node.removechild(`$selectedNode.node)}
				`$writerSettings = new-object System.Xml.XmlWriterSettings
				`$writerSettings.OmitXmlDeclaration = `$false
				`$writerSettings.NewLineOnAttributes = `$false
				`$writerSettings.Indent = `$true			
				`$writer = [System.Xml.XmlWriter]::Create(`$appConfigFile, `$writerSettings)
				`$appConfig.WriteTo(`$writer)
				`$writer.Flush()
				`$writer.Close()
			}
		}
	}
	
if(`$Host.Version.Major -gt 1)
{  
	[xml] `$prjXml = Get-Content `$project.FullName
	`$proceed = `$true
	foreach(`$PropertyGroup in `$prjXml.project.ChildNodes)
	{
	  
	  if(`$PropertyGroup.StartAction -ne `$null)
	  {
		`$proceed = `$false
	  }
	  
	}

	if (`$proceed -eq `$true){
		`$propertyGroupElement = `$prjXml.CreateElement(`"PropertyGroup`");
		`$propertyGroupElement.SetAttribute(`"Condition`", `"'```$(Configuration)|```$(Platform)' == 'Release|AnyCPU'`")
		`$propertyGroupElement.RemoveAttribute(`"xmlns`")
		`$startActionElement = `$prjXml.CreateElement(`"StartAction`");
		`$propertyGroupElement.AppendChild(`$startActionElement)
		`$propertyGroupElement.StartAction = `"Program`"
		`$startProgramElement = `$prjXml.CreateElement(`"StartProgram`");
		`$propertyGroupElement.AppendChild(`$startProgramElement)
		`$propertyGroupElement.StartProgram = `"```$(ProjectDir)```$(OutputPath)NServiceBus.Host.exe`"
		`$prjXml.project.AppendChild(`$propertyGroupElement);
		`$writerSettings = new-object System.Xml.XmlWriterSettings
		`$writerSettings.OmitXmlDeclaration = `$true
		`$writerSettings.NewLineOnAttributes = `$true
		`$writerSettings.Indent = `$true
		`$projectFilePath = Resolve-Path -Path `$project.FullName
		`$writer = [System.Xml.XmlWriter]::Create(`$projectFilePath, `$writerSettings)

		`$prjXml.WriteTo(`$writer)
		`$writer.Flush()
		`$writer.Close()
	}
}
else{
	echo `"Please use PowerShell V2 for better configuration for the project`"
} 
"
	$appConfigTranformFile = "$releaseRoot\content\app.config.transform"
	$installPs1File = "$releaseRoot\tools\install.ps1"
	Create-Directory "$releaseRoot\content"
	Write-Output $appConfigTranformContent > $appConfigTranformFile	

	Write-Output $installPs1Content > $installPs1File	
	
	$packageName = "NServiceBus.Host" + $PackageNameSuffix
	$packit.package_description = "The hosting template for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $script:packageVersion @{$packageNameNsb=$script:packageVersion} "" @{".\release\net40\binaries\NServiceBus.Host.*"="lib\net40";
																									 ".\release\content\*.*"="content";
																									  ".\release\tools\install.ps1*"="tools"}
	#endregion

	#region Packing NServiceBus.Host32
	$packageName = "NServiceBus.Host32" + $PackageNameSuffix
	$packit.package_description = "The hosting template for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $script:packageVersion @{$packageNameNsb=$script:packageVersion} "" @{".\release\net40\binaries\NServiceBus.Host32.*"="lib\net40\x86";
																									 ".\release\content\*.*"="content";
																									  ".\release\tools\install.ps1*"="tools"}
	Remove-Item -Force $appConfigTranformFile -ErrorAction SilentlyContinue
	Remove-Item -Force $installPs1File -ErrorAction SilentlyContinue
	Delete-Directory "$releaseRoot\content"
	#endregion
	
	#region Packing NServiceBus.Testing
	$packageName = "NServiceBus.Testing" + $PackageNameSuffix
	$packit.package_description = "The testing for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $script:packageVersion @{$packageNameNsb=$script:packageVersion} "binaries\NServiceBus.Testing.dll"
	#endregion
	
	#region Packing NServiceBus.Tools
	$runMeFirstFileContent = ".\msmqutils\runner.exe %1"
	$runMeFirstFile = "$releaseRoot\tools\RunMeFirst.bat"
	Write-Output $runMeFirstFileContent > $runMeFirstFile
	
	$installPs1Content = "param(`$installPath, `$toolsPath, `$package, `$project)
    echo `"The Tools Path (`$toolsPath) has been added to the env:PATH. Please use RunMeFirst.bat and returntosourcequeue.exe directly in Package Manager Console`"
"
	$installPs1File = "$releaseRoot\tools\init.ps1"
	$installPs1Content > $installPs1File
	$packageName = "NServiceBus.Tools" + $PackageNameSuffix
	$packit.package_description = "The tools to configure the nservicebus"
	invoke-packit $packageName $script:packageVersion @{} "" @{".\release\tools\msmqutils\*.*"="tools\msmqutils";
															   ".\release\tools\*.dll"="tools";".\release\tools\*.*"="tools";}
	
	Remove-Item -Force $runMeFirstFile -ErrorAction SilentlyContinue
	Remove-Item -Force $installPs1File -ErrorAction SilentlyContinue
	#endregion
	
	#region Packing NServiceBus.Integration.WebServices
	$packageName = "NServiceBus.Integration.WebServices" + $PackageNameSuffix
	$packit.package_description = "The WebServices Integration for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit $packageName $script:packageVersion @{$packageNameNsb=$script:packageVersion} "binaries\NServiceBus.Integration.WebServices.dll"
	#endregion

	#region Packing NServiceBus.Autofac
	$packageName = "NServiceBus.Autofac" + $PackageNameSuffix
	$packit.package_description = "The Autofac Container for the nservicebus"
	invoke-packit $packageName $script:packageVersion @{"Autofac"="2.6.1.841"} "" @{".\release\net40\binaries\containers\autofac\*.*"="lib\net40"}
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
	invoke-packit $packageName $script:packageVersion @{"Ninject"="3.0.0.15";"Ninject.Extensions.ContextPreservation"="3.0.0.8";"Ninject.Extensions.NamedScope"="3.0.0.5"} "" @{".\release\net40\binaries\containers\Ninject\*.*"="lib\net40"}
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
	$packageNameAzure = "NServiceBus.Azure" + $PackageNameSuffix
	$packit.package_description = "Azure support for NServicebus"
	invoke-packit $packageNameAzure $script:packageVersion @{$packageNameNsb=$script:packageVersion; $packageNameNHibernate=$script:packageVersion; "Common.Logging"="2.0.0";"Newtonsoft.Json"="4.0.5" } "binaries\NServiceBus.Azure.dll", "..\..\lib\ServiceLocation\Microsoft.Practices.ServiceLocation.dll", 
	"..\..\lib\azure\Microsoft.WindowsAzure.Diagnostics.dll", "..\..\lib\azure\Microsoft.WindowsAzure.ServiceRuntime.dll", "..\..\lib\azure\Microsoft.WindowsAzure.StorageClient.dll", "..\..\lib\azure\Microsoft.ServiceBus.dll","..\..\lib\NHibernate.Drivers.Azure.TableStorage.dll","..\..\lib\Ionic.Zip.dll" 
	#endregion	
	
	#region Packing NServiceBus.Hosting.Azure
	$packageNameHostingAzure = "NServiceBus.Hosting.Azure" + $PackageNameSuffix
	$packit.package_description = "The Azure Host for NServicebus"
	invoke-packit $packageNameHostingAzure $script:packageVersion @{$packageNameAzure=$script:packageVersion; } "binaries\NServiceBus.Hosting.Azure.dll" @{}
	#endregion
	
	#region Packing NServiceBus.Timeout.Hosting.Azure
	$packageNameTimeoutHostingAzure = "NServiceBus.Timeout.Hosting.Azure" + $PackageNameSuffix
	$packit.package_description = "The Azure Host for the timeout manager on NServicebus"
	invoke-packit $packageNameTimeoutHostingAzure $script:packageVersion @{$packageNameHostingAzure=$script:packageVersion; } "binaries\NServiceBus.Timeout.Hosting.Azure.dll" @{}
	#endregion
	
	#region Packing NServiceBus.Hosting.Azure.HostProcess
	$packageNameHostingAzureHostProcess = "NServiceBus.Hosting.Azure.HostProcess" + $PackageNameSuffix
	$packit.package_description = "The process used when sharing an azure instance between multiple NServicebus endpoints"
	invoke-packit $packageNameHostingAzureHostProcess $script:packageVersion @{$packageNameHostingAzure=$script:packageVersion; } "binaries\NServiceBus.Hosting.Azure.HostProcess.exe"
	#endregion
		
	remove-module packit
 }

task PrepareReleaseWithoutSamples -depends PrepareBinaries -description "Prepare release without compiling the sample projects"  {
	
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
}

task DetectOperatingSystemArchitecture -description "Detects the OS architecture " {
	if (IsWow64 -eq $true)
	{
		$script:architecture = "x64"
	}
    echo "Machine Architecture is $script:architecture"
}
  
task GenerateAssemblyInfo -description "Generates assembly info for all the projects with version" {
	if($env:BUILD_NUMBER -ne $null) {
    	$BuildNumber = $env:BUILD_NUMBER
	}
	Write-Output "Build Number: $BuildNumber"
	
	$asmVersion =  $ProductVersion + ".0.0"

	if($PreRelease -eq "") {
		$fileVersion = $ProductVersion + "." + $BuildNumber + ".0" 
		$infoVersion = $fileVersion
		$script:packageVersion = $ProductVersion + "." + $BuildNumber
	}
	else {
		$fileVersion = $ProductVersion + "." + $PatchVersion + "." + $BuildNumber 
		$infoVersion = $ProductVersion+ "." + $PatchVersion + $PreRelease + $BuildNumber 
		$script:packageVersion = $infoVersion
	}
	
	#Temporarily removed the PreRelease prefix ('-build') from the package version for CI packages to maintain compatibility with the existing versioning scheme
	#We will remove this as soon as we until we consolidate the CI and regular packages
	if($PackageNameSuffix -eq "-CI") {
		$script:packageVersion = $ProductVersion + "." + $BuildNumber
	}

	$script:releaseVersion = $script:packageVersion
		
	Write-Output "##teamcity[buildNumber '$script:releaseVersion']"
	
	$projectFiles = ls -path $srcDir -include *.csproj -recurse  
	$projectFiles += ls -path $baseDir\tests -include *.csproj -recurse  

	foreach($projectFile in $projectFiles) {

		$projectDir = [System.IO.Path]::GetDirectoryName($projectFile)
		$projectName = [System.IO.Path]::GetFileName($projectDir)
		$asmInfo = [System.IO.Path]::Combine($projectDir, [System.IO.Path]::Combine("Properties", "AssemblyInfo.cs"))
		
		$assemblyTitle = gc $asmInfo | select-string -pattern "AssemblyTitle"
		
		if($assemblyTitle -ne $null){
			$assemblyTitle = $assemblyTitle.ToString()
			if($assemblyTitle -ne ""){
				$assemblyTitle = $assemblyTitle.Replace('[assembly: AssemblyTitle("', '') 
				$assemblyTitle = $assemblyTitle.Replace('")]', '') 
				$assemblyTitle = $assemblyTitle.Trim()
				
			}
		}
		else{
			$assemblyTitle = ""	
		}
		
		$assemblyDescription = gc $asmInfo | select-string -pattern "AssemblyDescription" 
		if($assemblyDescription -ne $null){
			$assemblyDescription = $assemblyDescription.ToString()
			if($assemblyDescription -ne ""){
				$assemblyDescription = $assemblyDescription.Replace('[assembly: AssemblyDescription("', '') 
				$assemblyDescription = $assemblyDescription.Replace('")]', '') 
				$assemblyDescription = $assemblyDescription.Trim()
			}
		}
		else{
			$assemblyDescription = ""
		}
		
		
		$assemblyProduct =  gc $asmInfo | select-string -pattern "AssemblyProduct" 
		
		if($assemblyProduct -ne $null){
			$assemblyProduct = $assemblyProduct.ToString()
			if($assemblyProduct -ne ""){
				$assemblyProduct = $assemblyProduct.Replace('[assembly: AssemblyProduct("', '') 
				$assemblyProduct = $assemblyProduct.Replace('")]', '') 
				$assemblyProduct = $assemblyProduct.Trim()
			}
		}
		else{
			$assemblyProduct = "NServiceBus"
		}
		
		$internalsVisibleTo = gc $asmInfo | select-string -pattern "InternalsVisibleTo" 
		
		if($internalsVisibleTo -ne $null){
			$internalsVisibleTo = $internalsVisibleTo.ToString()
			if($internalsVisibleTo -ne ""){
				$internalsVisibleTo = $internalsVisibleTo.Replace('[assembly: InternalsVisibleTo("', '') 
				$internalsVisibleTo = $internalsVisibleTo.Replace('")]', '') 
				$internalsVisibleTo = $internalsVisibleTo.Trim()
			}
		}
		else{
			$assemblyProduct = "NServiceBus"
		}
		
		$notclsCompliant = @("")

		$clsCompliant = (($projectDir.ToString().StartsWith("$srcDir")) -and ([System.Array]::IndexOf($notclsCompliant, $projectName) -eq -1)).ToString().ToLower()
		
		Generate-Assembly-Info $assemblyTitle `
		$assemblyDescription  `
		$clsCompliant `
		$internalsVisibleTo `
		"release" `
		"NServiceBus" `
		$assemblyProduct `
		"Copyright (C) NServiceBus 2010-2012" `
		$asmVersion `
		$fileVersion `
		$infoVersion `
		$asmInfo 
 	}
}

task InstallDependentPackages -description "Installs dependent packages in the environment if required" {
	cd "$baseDir\packages"
	$files =  dir -Exclude *.config
	cd $baseDir
	$installDependentPackages = $DownloadDependentPackages;
	if($installDependentPackages -eq $false){
		$installDependentPackages = ((($files -ne $null) -and ($files.count -gt 0)) -eq $false)
	}
	if($installDependentPackages){
    $packageNames = @{}
    
    dir -recurse -include ('packages.config') | ForEach-Object {
			$packageconfig = [io.path]::Combine($_.directory,$_.name)

      $packagexml = [xml] (gc $packageconfig)
    
      foreach ($pelem in $packagexml.packages.package) {
        if ($pelem.id -eq $null) { continue }
      
        $packageNames["{0}-{1}" -f $pelem.id, $pelem.version] = $pelem
      }
		}

    $template = [xml] ("<packages></packages>")

    $packageNames.GetEnumerator() | Sort-Object Name | % { $template.DocumentElement.AppendChild($template.ImportNode($_.Value, $true)) | Out-Null }

    $template.Save( (Join-Path (pwd) packages\packages.config) )
    exec { &$nugetExec install packages\packages.config -o packages }
    rm packages\packages.config
  }
 }

task ReleaseNServiceBus -depends PrepareRelease, CreatePackages, ZipOutput -description "After preparing for Release creates the nuget packages, if UploadPackage is set to true then publishes the packages to Nuget gallery and compress and release artifacts "  {
    if(Test-Path -Path $releaseDir)
	{
        del -Path $releaseDir -Force -recurse
	}	
	echo "Release completed for NServiceBus." + $script:releaseVersion 
	
	Stop-Process -Name "nunit-agent.exe" -ErrorAction SilentlyContinue -Force
	Stop-Process -Name "nunit-console.exe" -ErrorAction SilentlyContinue -Force
}

task ZipOutput -description "Ziping artifacts directory for releasing"  {
	
	echo "Cleaning the Release Artifacts before ziping"
	$packagingArtifacts = "$releaseRoot\PackagingArtifacts"
	$packageOutPutDir = "$releaseRoot\packages"
	
	if(Test-Path -Path $packagingArtifacts ){
		Delete-Directory $packagingArtifacts
	}
	Copy-Item -Force -Recurse $releaseDir\binaries "$releaseRoot\binaries"  -ErrorAction SilentlyContinue  
	Copy-Item -Force -Recurse $releaseDir\packages "$releaseRoot\packages"  -ErrorAction SilentlyContinue  
	
	Delete-Directory $releaseDir
			
	if((Test-Path -Path $packageOutPutDir) -and ($UploadPackage) ){
        Delete-Directory $packageOutPutDir
	}

	if((Test-Path -Path $artifactsDir) -eq $true)
	{
		Delete-Directory $artifactsDir
	}
	
    Create-Directory $artifactsDir
	
	$archive = "$artifactsDir\NServiceBus.$script:releaseVersion.zip"
	$archiveCoreOnly = "$artifactsDir\NServiceBusCore-Only.$script:releaseVersion.zip"
	echo "Ziping artifacts directory for releasing"
	exec { &$zipExec a -tzip $archive $releaseRoot\** }
	exec { &$zipExec a -tzip $archiveCoreOnly $coreOnlyDir\** }
	
}

task UpdatePackages -description "Updates the packages in packages.config of all the solutions"  {
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