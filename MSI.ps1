properties {
	$ProductVersion = "4.0"
	$PatchVersion = "0"
	$BuildNumber = "0"
	$VsixFilePath = if($env:VSIX_PATH -ne $null) { $env:VSIX_PATH } else { "C:\Projects" }
}

$baseDir = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
$packageOutPutDir = "$baseDir\artifacts"
$toolsDir = "$baseDir\tools"
$buildWixPath = "$baseDir\build\wix\"

include $toolsDir\psake\buildutils.ps1

task default -depends Build

task Clean { 
	if ( -Not (Test-Path $packageOutPutDir))
	{
		New-Item $packageOutPutDir -ItemType Directory | Out-Null
	} else {
		Remove-Item ($packageOutPutDir + '\*.msi')
	}
}

task Init {
	
		$netfxInstallroot = "" 
		$netfxInstallroot =	Get-RegistryValue 'HKLM:\SOFTWARE\Microsoft\.NETFramework\' 'InstallRoot' 
			
		$netfxCurrent = $netfxInstallroot + "v4.0.30319"
			
		$script:msBuild = $netfxCurrent + "\msbuild.exe"
			
		echo ".Net 4.0 build requested - $script:msBuild" 
}

task Build -depends Clean, Init {
	exec { &$script:msBuild $baseDir\src\wix\WixSolution.sln /t:"Clean,Build" /p:OutDir="$buildWixPath" /p:Configuration=Release /p:ProductVersion="$ProductVersion.$PatchVersion.$BuildNumber" /p:VsixPath="$VsixFilePath" }
	copy $buildWixPath*.msi $packageOutPutDir\
}