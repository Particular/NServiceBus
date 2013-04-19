properties {
	$ProductVersion = "4.0"
	$PatchVersion = "0"
	$BuildNumber = "0"
	$PreRelease = ""
	$SignFile = if($env:SIGN_CER_PATH -ne $null) { $env:SIGN_CER_PATH } else { "" }
}

$baseDir = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
$releaseRoot = "$baseDir\Release"
$outputDir = "$baseDir\NServiceBus Setup\Output Package"
$toolsDir = "$baseDir\tools"
$projectFile = "$baseDir\NServiceBus Setup\NServiceBus.aip"

include $toolsDir\psake\buildutils.ps1

task default -depends Init, Build, Sign

task Init {

	$sdkInstallRoot = Get-RegistryValue "HKLM:\SOFTWARE\Microsoft\Microsoft SDKs\Windows\v7.1" "InstallationFolder"
	if($sdkInstallRoot -eq $null) {
		$sdkInstallRoot = Get-RegistryValue "HKLM:\SOFTWARE\Microsoft\Microsoft SDKs\Windows\v7.0A" "InstallationFolder"
	}

	$script:signTool = $sdkInstallRoot + "Bin\signtool.exe"

    # Install path for Advanced Installer
    $AdvancedInstallerPath = ""
    $AdvancedInstallerPath = Get-RegistryValue "HKLM:\SOFTWARE\Wow6432Node\Caphyon\Advanced Installer\" "Advanced Installer Path" 
    $script:AdvinstCLI = $AdvancedInstallerPath + "\bin\x86\AdvancedInstaller.com"
    
	robocopy "$baseDir\Release" "C:\Projects\NServiceBus\NServiceBus Setup\Files" /E
}

task Build -depends {
	#$UpgradeCode = "6bf2f238-54fb-4300-ab68-2416491af0" + $ProductVersion.Replace(".", "")

    if($PreRelease -eq "") {
		$archive = "NServiceBus.$ProductVersion.$PatchVersion" 
	} else {
		$archive = "NServiceBus.$ProductVersion.$PatchVersion-$PreRelease$BuildNumber"
	}

	exec { &$script:AdvinstCLI /edit $projectFile /ResetSync APPDIR\NServiceBus\Binaries }
	exec { &$script:AdvinstCLI /edit $projectFile /ResetSync APPDIR\NServiceBus\Samples }
	exec { &$script:AdvinstCLI /edit $projectFile /ResetSync APPDIR\NServiceBus\Tools }
	exec { &$script:AdvinstCLI /edit $projectFile /NewSync APPDIR\NServiceBus\Binaries $baseDir\NServiceBus Setup\Files\binaries }
	exec { &$script:AdvinstCLI /edit $projectFile /NewSync APPDIR\NServiceBus\Samples $baseDir\NServiceBus Setup\Files\samples }
	exec { &$script:AdvinstCLI /edit $projectFile /NewSync APPDIR\NServiceBus\Tools $baseDir\NServiceBus Setup\Files\tools }
	# edit Advanced Installer Project	  
	exec { &$script:AdvinstCLI /edit $projectFile /SetVersion "$ProductVersion.$PatchVersion" -noprodcode }	
	exec { &$script:AdvinstCLI /edit $projectFile /SetPackageName "$archive.exe" -buildname DefaultBuild }
		
	# Build setup with Advanced Installer	
	exec { &$script:AdvinstCLI /rebuild $projectFile }
}

task Sign -depends Init {
	if($SignFile -ne "") {
		exec { &$script:signTool sign /f "$SignFile" /p "$env:SIGN_CER_PASSWORD" /d "NServiceBus Installer" /du "http://www.nservicebus.com" /q  $outputDir\*.* }
	}
}

