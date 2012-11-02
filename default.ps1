properties {
	$ProductVersion = "4.0"
    $PatchVersion = "0"
	$BuildNumber = if($env:BUILD_NUMBER -ne $null) { $env:BUILD_NUMBER } else { "0" }
	$PreRelease = "alpha"
	$TargetFramework = "net-4.0"
	$buildConfiguration = "Debug"
    $NugetKey = ""
	$UploadPackage = $false
}

$baseDir = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
$releaseRoot = "$baseDir\Release"
$releaseDir = "$releaseRoot\net40"
$packageOutPutDir = "$baseDir\artifacts"
$toolsDir = "$baseDir\tools"
$srcDir = "$baseDir\src"
$binariesDir = "$baseDir\binaries"
$coreOnlyDir = "$baseDir\core-only"
$coreOnlyBinariesDir = "$coreOnlyDir\binaries"
$outDir = "$baseDir\build"
$buildBase = "$baseDir\build"
$libDir = "$baseDir\lib" 
$artifactsDir = "$baseDir\artifacts"
$nunitexec = "packages\NUnit.2.5.10.11092\tools\nunit-console.exe"
$zipExec = "$toolsDir\zip\7za.exe"
$ilMergeKey = "$srcDir\NServiceBus.snk"
$ilMergeExclude = "$toolsDir\IlMerge\ilmerge.exclude"
$script:ilmergeTargetFramework = ""
$script:msBuildTargetFramework = ""	
$script:nunitTargetFramework = "/framework=4.0";

include $toolsDir\psake\buildutils.ps1

task default -depends Build

task Clean { 
	if(Test-Path $binariesDir){
		Delete-Directory $binariesDir
	}

	if(Test-Path $artifactsDir){
		Delete-Directory $artifactsDir
	}
}

task Init {
	
		$netfxInstallroot ="" 
		$netfxInstallroot =	Get-RegistryValue 'HKLM:\SOFTWARE\Microsoft\.NETFramework\' 'InstallRoot' 
			
		$netfxCurrent = $netfxInstallroot + "v4.0.30319"
			
		$script:msBuild = $netfxCurrent + "\msbuild.exe"
			
		echo ".Net 4.0 build requested - $script:msBuild" 

		$script:ilmergeTargetFramework  = "/targetplatform:v4," + $netfxCurrent

		$ilMergeTargetFrameworkPath = (get-item 'Env:\ProgramFiles').value + '\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0'
		if(test-path $ilMergeTargetFrameworkPath) {
			$script:ilmergeTargetFramework = "/targetplatform:v4," + $ilMergeTargetFrameworkPath		
		} else {
			$ilMergeTargetFrameworkPath = (get-item 'Env:\ProgramFiles(x86)').value + '\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0'

			if(test-path $ilMergeTargetFrameworkPath) {
				$script:ilmergeTargetFramework = "/targetplatform:v4," + $ilMergeTargetFrameworkPath
			}
		}
			
		$script:msBuildTargetFramework ="/p:TargetFrameworkVersion=v4.0 /ToolsVersion:4.0"
			
		$script:nunitTargetFramework = "/framework=4.0";
			
		Set-Item -path env:COMPLUS_Version -value "v4.0.30319"

		if(-not (Test-Path $binariesDir)){	
			Create-Directory $binariesDir
		}

		if(-not (Test-Path $artifactsDir)){	
			Create-Directory $artifactsDir
		}
}

task PrepareBinaries -depends CopyBinaries {
		
	
}

task CopyBinaries -depends Merge {
		
	Copy-Item $outDir\log4net.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.??? $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.Azure.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.Host.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.Hosting.Azure.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.Hosting.Azure.HostProcess.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.NHibernate.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.Testing.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.Timeout.Hosting.Azure.* $binariesDir -Force -Exclude **Tests.dll
	
	Create-Directory "$binariesDir\containers\autofac"
	Copy-Item "$outDir\NServiceBus.ObjectBuilder.Autofac.*"  $binariesDir\containers\autofac -Force -Exclude **Tests.dll
	
	Create-Directory "$binariesDir\containers\castle"
	Copy-Item "$outDir\NServiceBus.ObjectBuilder.CastleWindsor.*"  $binariesDir\containers\castle -Force -Exclude **Tests.dll
	
	Create-Directory "$binariesDir\containers\structuremap"
	Copy-Item "$outDir\NServiceBus.ObjectBuilder.StructureMap.*"  $binariesDir\containers\structuremap -Force -Exclude **Tests.dll
	
	Create-Directory "$binariesDir\containers\spring"
	Copy-Item "$outDir\NServiceBus.ObjectBuilder.Spring.*"  $binariesDir\containers\spring -Force -Exclude **Tests.dll
			
	Create-Directory "$binariesDir\containers\unity"
	Copy-Item "$outDir\NServiceBus.ObjectBuilder.Unity.*"  $binariesDir\containers\unity -Force -Exclude **Tests.dll
		
	Create-Directory "$binariesDir\containers\ninject"
	Copy-Item "$outDir\NServiceBus.ObjectBuilder.Ninject.*"  $binariesDir\containers\ninject -Force	-Exclude **Tests.dll
}

task Build -depends Clean, Init {
	exec { &$script:msBuild $baseDir\NServiceBus.sln /t:"Clean,Build" /p:Configuration=Release /p:OutDir="$outDir\" }
}

task RunTests -depends Build {
	
	if((Test-Path -Path $buildBase\test-reports) -eq $false){
		Create-Directory $buildBase\test-reports 
	}	
	
	if ( -Not (Test-Path $env:temp\filestoexclude))
	{
		Create-Directory $env:temp\filestoexclude
	} else {
		Remove-Item $env:temp\filestoexclude\*.*
	}

	Move-Item -path $buildBase\*.exe -destination $env:temp\filestoexclude\ -Force
	
	$testAssemblies = @()
	$testAssemblies +=  dir $buildBase\*Tests.dll
	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework}

	Move-Item -path $env:temp\filestoexclude\*.exe -destination $buildBase\ -Force
}

task Merge -depends Init, RunTests {

	$assemblies = @()
	$assemblies += dir $buildBase\NServiceBus.Core.dll
	$assemblies += dir $buildBase\NServiceBus.Setup.Windows.dll
	$assemblies += dir $buildBase\log4net.dll
	$assemblies += dir $buildBase\Interop.MSMQ.dll
	$assemblies += dir $buildBase\AutoFac.dll
	$assemblies += dir $buildBase\NLog.dll
	$assemblies += dir $buildBase\Raven.Abstractions.dll
	$assemblies += dir $buildBase\Raven.Client.Lightweight.dll
	$assemblies += dir $buildBase\rhino.licensing.dll
	$assemblies += dir $buildBase\Newtonsoft.Json.dll

	Ilmerge $ilMergeKey $binariesDir "NServiceBus.Core" $assemblies "" "dll"  $script:ilmergeTargetFramework "$buildBase\NServiceBusCoreMergeLog.txt"  $ilMergeExclude
}

task CompileSamples -depends CopyBinaries {
	$excludeFromBuild = @("AsyncPagesMVC3.sln", "AzureFullDuplex.sln", "AzureHost.sln", "AzurePubSub.sln", "AzureThumbnailCreator.sln", 
						  "ServiceBusFullDuplex.sln", "AzureServiceBusPubSub.sln", "ServiceBusPubSub.sln")
	$solutions = ls -path $baseDir\Samples -include *.sln -recurse  
		$solutions | % {
			$solutionName =  [System.IO.Path]::GetFileName($_.FullName)
				if([System.Array]::IndexOf($excludeFromBuild, $solutionName) -eq -1){
					$solutionFile = $_.FullName
					exec { &$script:msBuild /nr:true $solutionFile }
				}
		}
}
	
task CreatePackages {
    Invoke-psake .\Nuget.ps1 -properties @{PreRelease=$PreRelease;buildConfiguration=$buildConfiguration;PatchVersion=$PatchVersion;BuildNumber=$BuildNumber;ProductVersion=$ProductVersion;NugetKey=$NugetKey;UploadPackage=$UploadPackage}
}

task ZipOutput -description "Ziping artifacts directory for releasing"  {	
	$packagingArtifacts = "$releaseRoot\PackagingArtifacts"
	
	if(Test-Path -Path $packagingArtifacts ){
        del ($packagingArtifacts + '\*.zip')
	}
	Copy-Item -Force -Recurse $releaseDir\binaries "$releaseRoot\binaries"  -ErrorAction SilentlyContinue  
	
	Delete-Directory $releaseDir
	$archive = "$artifactsDir\NServiceBus.$script:releaseVersion.zip"
	$archiveCoreOnly = "$artifactsDir\NServiceBusCore-Only.$script:releaseVersion.zip"
	echo "Ziping artifacts directory for releasing"
	exec { &$zipExec a -tzip $archive $releaseRoot\** }
	exec { &$zipExec a -tzip $archiveCoreOnly $coreOnlyDir\** }
}

task CreateMSI {
    Invoke-psake .\MSI.ps1 -properties @{ProductVersion=$ProductVersion;PatchVersion=$PatchVersion}
}