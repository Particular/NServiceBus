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
$packageOutPutDir = "$baseDir\artifacts"
$toolsDir = "$baseDir\tools"
$srcDir = "$baseDir\src"
$binariesDir = "$baseDir\binaries"
$coreOnlyDir = "$baseDir\core-only"
$coreOnlyBinariesDir = "$coreOnlyDir\binaries"
$outDir = "$baseDir\build"
$outDir32 = "$baseDir\build32"
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

task default -depends PrepareBinaries

task PrepareBinaries -depends CopyBinaries

task CreateRelease -depends GenerateAssemblyInfo, Build, Merge, PrepareBinaries, CreateReleaseFolder, CreateMSI, ZipOutput, CreatePackages

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
			$script:ilmergeTargetFramework = '/targetplatform:v4,' + $ilMergeTargetFrameworkPath
		} else {
			$ilMergeTargetFrameworkPath = (get-item 'Env:\ProgramFiles(x86)').value + '\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0'

			if(test-path $ilMergeTargetFrameworkPath) {
				$script:ilmergeTargetFramework = '/targetplatform:v4,' + $ilMergeTargetFrameworkPath
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

task GenerateAssemblyInfo -description "Generates assembly info for all the projects with version" {

	Write-Output "Build Number: $BuildNumber"
	
	$asmVersion =  $ProductVersion + ".0.0"

	if($PreRelease -eq "") {
		$fileVersion = $ProductVersion + "." + $PatchVersion + ".0" 
		$infoVersion = $ProductVersion + "." + $PatchVersion
	} else {
		$fileVersion = $ProductVersion + "." + $PatchVersion + "." + $BuildNumber 
		$infoVersion = $ProductVersion + "." + $PatchVersion + "-" + $PreRelease + $BuildNumber 	
	}
    
	Write-Output "##teamcity[buildNumber '$infoVersion']"
	
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
		
		$notclsCompliant = @("")

		$clsCompliant = (($projectDir.ToString().StartsWith("$srcDir")) -and ([System.Array]::IndexOf($notclsCompliant, $projectName) -eq -1)).ToString().ToLower()
		
		Generate-Assembly-Info $assemblyTitle `
		$assemblyDescription  `
		$clsCompliant `
		"" `
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

task CopyBinaries -depends Merge {
		
	Copy-Item $outDir\log4net.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.??? $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.Azure.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.Hosting.Azure.??? $binariesDir -Force -Exclude **Tests.dll, *.config
	Copy-Item $outDir\NServiceBus.NHibernate.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.Testing.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.Timeout.Hosting.Azure.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.PowerShell.* $binariesDir -Force -Exclude **Tests.dll
	Copy-Item $outDir\NServiceBus.Setup.Windows.* $binariesDir -Force -Exclude **Tests.dll
	
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

task CreateReleaseFolder {

	Delete-Directory $releaseRoot
	Create-Directory $releaseRoot

	Copy-Item $binariesDir $releaseRoot\binaries -Force -Recurse

	Copy-Item "$baseDir\*.txt" $releaseRoot -Force -ErrorAction SilentlyContinue
	Copy-Item "$baseDir\RunMeFirst.bat" $releaseRoot -Force -ErrorAction SilentlyContinue
	
	Create-Directory $releaseRoot\tools\licenseinstaller
	Copy-Item "$outDir\LicenseInstaller.exe" -Destination $releaseRoot\tools\licenseinstaller -Force -ErrorAction SilentlyContinue

	Create-Directory $releaseRoot\tools\migration
	Copy-Item "$binariesDir\NServiceBus.dll" -Destination $releaseRoot\tools\migration -Force 
	Copy-Item "$binariesDir\NServiceBus.Core.dll" -Destination $releaseRoot\tools\migration -Force 
	Copy-Item "$outDir\NServiceBus.Compatibility.dll" -Destination $releaseRoot\tools\migration -Force -ErrorAction SilentlyContinue
	Copy-Item "$outDir\TimeoutMigrator.exe" -Destination $releaseRoot\tools\migration -Force -ErrorAction SilentlyContinue
	
	Copy-Item "$binariesDir\NServiceBus.Core.dll" -Destination $releaseRoot\tools -Force -ErrorAction SilentlyContinue
	Copy-Item "$binariesDir\NServiceBus.dll" -Destination $releaseRoot\tools -Force -ErrorAction SilentlyContinue
	Copy-Item "$outDir\ReturnToSourceQueue.exe" -Destination $releaseRoot\tools -Force -ErrorAction SilentlyContinue
	Copy-Item "$outDir\XsdGenerator.exe" -Destination $releaseRoot\tools -Force -ErrorAction SilentlyContinue
	
	Copy-Item -Force -Recurse "$baseDir\Samples" $releaseRoot\samples  -ErrorAction SilentlyContinue 
	dir "$releaseRoot\samples" -recurse -include ('bin', 'obj', 'packages') | ForEach-Object {
		write-host deleting $_ 
		Delete-Directory $_
	}
}

task Build -depends Clean, Init {
	exec { &$script:msBuild $baseDir\NServiceBus.sln /t:"Clean,Build" /p:Platform="Any CPU" /p:Configuration=Release /p:OutDir="$outDir\" /m }
	exec { &$script:msBuild $baseDir\NServiceBus.sln /t:"Clean,Build" /p:Platform="x86" /p:Configuration=Release /p:OutDir="$outDir32\" /m }

	del $binariesDir\*.xml -Recurse
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
	$testAssemblies +=  dir $buildBase\*Tests.dll -Exclude NServiceBus.Integration.Azure.Tests.dll, NServiceBus.Unicast.Queuing.Azure.Tests.dll
	exec {&$nunitexec $testAssemblies $script:nunitTargetFramework}

	Move-Item -path $env:temp\filestoexclude\*.exe -destination $buildBase\ -Force
}

task Merge -depends Build {

	$assemblies = @()
	$assemblies += dir $outDir\NServiceBus.Core.dll
	$assemblies += dir $outDir\NServiceBus.Setup.Windows.dll
	$assemblies += dir $outDir\log4net.dll
	$assemblies += dir $outDir\Interop.MSMQ.dll
	$assemblies += dir $outDir\AutoFac.dll
	$assemblies += dir $outDir\NLog.dll
	$assemblies += dir $outDir\Raven.Abstractions.dll
	$assemblies += dir $outDir\Raven.Client.Lightweight.dll
	$assemblies += dir $outDir\rhino.licensing.dll
	$assemblies += dir $outDir\Newtonsoft.Json.dll

	Ilmerge $ilMergeKey $binariesDir "NServiceBus.Core.dll" $assemblies "library" $script:ilmergeTargetFramework $ilMergeExclude

	$assemblies = @()
	$assemblies += dir $outDir\NServiceBus.Host.exe
	$assemblies += dir $outDir\log4net.dll
	$assemblies += dir $outDir\Topshelf.dll
	$assemblies += dir $outDir\Microsoft.Practices.ServiceLocation.dll

	Ilmerge $ilMergeKey $binariesDir "NServiceBus.Host.exe" $assemblies "exe" $script:ilmergeTargetFramework $ilMergeExclude

	$assemblies = @()
	$assemblies += dir $outDir32\NServiceBus.Host.exe
	$assemblies += dir $outDir32\log4net.dll
	$assemblies += dir $outDir32\Topshelf.dll
	$assemblies += dir $outDir32\Microsoft.Practices.ServiceLocation.dll

	Ilmerge $ilMergeKey $binariesDir "NServiceBus.Host32.exe" $assemblies "exe" $script:ilmergeTargetFramework $ilMergeExclude

	$assemblies = @()
	$assemblies += dir $outDir\NServiceBus.Hosting.Azure.HostProcess.exe
	$assemblies += dir $outDir\log4net.dll
	$assemblies += dir $outDir\Topshelf.dll
	$assemblies += dir $outDir\Microsoft.Practices.ServiceLocation.dll
	
	Ilmerge $ilMergeKey $binariesDir "NServiceBus.Hosting.Azure.HostProcess.exe" $assemblies "exe" $script:ilmergeTargetFramework $ilMergeExclude
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

task ZipOutput {	
	
	$archive = "$artifactsDir\NServiceBus.$ProductVersion.$PatchVersion.zip"

	echo "Ziping artifacts directory for releasing"
	exec { &$zipExec a -tzip $archive $releaseRoot\** }
}

task CreateMSI {
    Invoke-psake .\MSI.ps1 -properties @{ProductVersion=$ProductVersion;PatchVersion=$PatchVersion}
}