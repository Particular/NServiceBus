properties {
	$ProductVersion = "4.0"
	$PatchVersion = "0"
	$BuildNumber = "0"
	$PreRelease = "alpha"
	$VsixFilePath = if($env:VSIX_PATH -ne $null) { $env:VSIX_PATH } else { "C:\Projects" }
	$SignFile = if($env:SIGN_CER_PATH -ne $null) { $env:SIGN_CER_PATH } else { "" }
}

$baseDir = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
$packageOutPutDir = "$baseDir\artifacts"
$toolsDir = "$baseDir\tools"
$buildWixPath = "$baseDir\buildwix\"
$heat = "$toolsDir\WiX\3.6\heat.exe"

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

task Build -depends Clean, Init, RunHeat {
	$UpgradeCode = "6bf2f238-54fb-4300-ab68-2416491af0" + $ProductVersion.Replace(".", "")

    if($PreRelease -eq "") {
		$archive = "NServiceBus.$ProductVersion.$PatchVersion"
	} else {
		$archive = "NServiceBus.$ProductVersion.$PatchVersion-$PreRelease$BuildNumber"
	}

	exec { &$script:msBuild $baseDir\src\wix\WixSolution.sln /t:"Clean,Build" /p:OutputName="$archive" /p:OutDir="$buildWixPath" /p:Configuration=Release /p:ProductVersion="$ProductVersion.$PatchVersion" /p:VsixPath="$VsixFilePath" /p:UpgradeCode="$UpgradeCode" }
	copy $buildWixPath*.msi $packageOutPutDir\
}

task RunHeat {
	exec { &$heat dir "$baseDir\Release\Samples" -ag -sreg -scom -dr "APPLICATIONFOLDER" -out $baseDir\src\wix\WixInstaller\Fragments\SampleFilesFragment.wxs -var var.SOURCEPATH.SAMPLES -sfrag -cg "SampleFiles" }
	exec { &$heat dir "$baseDir\Release\Binaries" -ag -sreg -scom -dr "APPLICATIONFOLDER" -out $baseDir\src\wix\WixInstaller\Fragments\BinaryFilesFragment.wxs -var var.SOURCEPATH.BINARIES -sfrag -cg "BinaryFiles" }
	exec { &$heat dir "$baseDir\Release\Tools" -ag -sreg -scom -dr "APPLICATIONFOLDER" -out $baseDir\src\wix\WixInstaller\Fragments\ToolsFilesFragment.wxs -var var.SOURCEPATH.TOOLS -sfrag -cg "ToolsFiles" }
}

task Sign -depends Init {
	if($SignFile -ne "") {
		exec { &$script:signTool sign /f "$SignFile" /p "$env:SIGN_CER_PASSWORD" /d "NServiceBus Installer" /du "http://www.nservicebus.com" /q  $packageOutPutDir\*.msi }
	}
}