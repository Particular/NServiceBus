properties {
	$ProductVersion = "4.0"
	$PatchVersion = "0"
	$VsixFilePath = if($env:VSIX_PATH -ne $null) { $env:VSIX_PATH } else { "C:\Projects" }
	$SignFile = if($env:SIGN_CER_PATH -ne $null) { $env:SIGN_CER_PATH } else { "" }
}

$baseDir = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
$packageOutPutDir = "$baseDir\artifacts"
$toolsDir = "$baseDir\tools"
$buildWixPath = "$baseDir\build\wix\"

include $toolsDir\psake\buildutils.ps1

task default -depends Build, Sign

task Clean { 
	if ( -Not (Test-Path $packageOutPutDir))
	{
		New-Item $packageOutPutDir -ItemType Directory | Out-Null
	} else {
		Remove-Item ($packageOutPutDir + '\*.msi')
	}
}

task Init {
	
		$sdkInstallRoot = Get-RegistryValue "HKLM:\SOFTWARE\Microsoft\Microsoft SDKs\Windows\v7.1" "InstallationFolder"
        echo "skdpath" $sdkInstallRoot
		if($sdkInstallRoot -eq $null) {
			$sdkInstallRoot = Get-RegistryValue "HKLM:\SOFTWARE\Microsoft\Microsoft SDKs\Windows\v7.0A" "InstallationFolder"
		}

		$netfxInstallroot = "" 
		$netfxInstallroot =	Get-RegistryValue "HKLM:\SOFTWARE\Microsoft\.NETFramework\" "InstallRoot" 
			
		$netfxCurrent = $netfxInstallroot + "v4.0.30319"
			
		$script:msBuild = $netfxCurrent + "\msbuild.exe"
		$script:signTool = $sdkInstallRoot + "Bin\signtool.exe"

		echo ".Net 4.0 build requested - $script:msBuild" 
}

task Build -depends Clean, Init {
	exec { &$script:msBuild $baseDir\src\wix\WixSolution.sln /t:"Clean,Build" /p:OutDir="$buildWixPath" /p:Configuration=Release /p:ProductVersion="$ProductVersion.$PatchVersion" /p:VsixPath="$VsixFilePath" }
	copy $buildWixPath*.msi $packageOutPutDir\
}

task Sign -depends Init {
	if($SignFile -ne "") {
		exec { &$script:signTool sign /f "$SignFile" /p "$env:SIGN_CER_PASSWORD" /d "NServiceBus Installer" /du "http://particular.net" /q  $packageOutPutDir\*.msi }
	}
}