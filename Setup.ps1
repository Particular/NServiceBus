properties {
	$ProductVersion = "4.0"
	$PatchVersion = "0"
	$BuildNumber = "0"
	$PreRelease = ""
}

$baseDir = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
$outputDir = "$baseDir\NServiceBus Setup\Output Package"
$toolsDir = "$baseDir\tools"
$setupProjectFile = "$baseDir\NServiceBus Setup\NServiceBus.aip"

include $toolsDir\psake\buildutils.ps1

task default -depends Init, Build

task Init {

    # Install path for Advanced Installer
    $AdvancedInstallerPath = ""
    $AdvancedInstallerPath = Get-RegistryValue "HKLM:\SOFTWARE\Wow6432Node\Caphyon\Advanced Installer\" "Advanced Installer Path" 
    $script:AdvinstCLI = $AdvancedInstallerPath + "\bin\x86\AdvancedInstaller.com"
}

task Build {
	# $UpgradeCode = "6bf2f238-54fb-4300-ab68-2416491af0" + $ProductVersion.Replace(".", "")

    if($PreRelease -eq "") {
		$archive = "Particular.NServiceBus-$ProductVersion.$PatchVersion" 
        $preReleaseName = ""
	} else {
		$archive = "Particular.NServiceBus-$ProductVersion.$PatchVersion-$PreRelease$BuildNumber"
        $preReleaseName = "-$PreRelease$BuildNumber"
	}

	# edit Advanced Installer Project	  
	exec { &$script:AdvinstCLI /edit $setupProjectFile /SetVersion "$ProductVersion.$PatchVersion" -noprodcode }	
	exec { &$script:AdvinstCLI /edit $setupProjectFile /SetPackageName "$archive.exe" -buildname DefaultBuild }
	exec { &$script:AdvinstCLI /edit $setupProjectFile /SetOutputLocation -buildname DefaultBuild -path "$outputDir" }
	
    exec { &$script:AdvinstCLI /edit $setupProjectFile /SetProperty OPT_PRERELEASE_NAME="$preReleaseName" }
    
	# Build setup with Advanced Installer	
	exec { &$script:AdvinstCLI /rebuild $setupProjectFile }
}

