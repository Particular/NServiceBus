param($installPath, $toolsPath, $package, $project)

if($toolsPath){
	if (Get-Module NServiceBus.Powershell) {
		Remove-Module NServiceBus.Powershell.Development
	}

	$pathToNServiceBusPSCmdLets = Join-Path $toolsPath NServiceBus.Powershell.Development.dll

	if(Test-Path $pathToNServiceBusPSCmdLets){
		Import-Module $pathToNServiceBusPSCmdLets

		Write-Host ""
		Write-Host "Type 'get-help about_NServiceBus' to see all available NServiceBus commands."
		Write-Host ""
	}
	else {
		Write-Host "NServiceBus powershell module could not be found, no powershell commands will be available"	
	}
}



$nserviceBusKeyPath =  "HKCU:SOFTWARE\NServiceBus" 

$platformKeyPath = "HKCU:SOFTWARE\ParticularSoftware"
$isNewUser = $true

$packageVersion = "Unknown"

if($package){
	$packageVersion = $package.Version
}

#Figure out if this is a first time user
try {

	#Check for existing NServiceBus installations
	if (Test-Path $nserviceBusKeyPath) {
		"Existing NServiceBus v($packageVersion) user detected"
	
		$isNewUser = $false
	}
	
	if (Test-Path $platformKeyPath){
		"Existing Platform user detected"
		$isNewUser = $false
	}

	if (!($isNewuser)) {
		exit
	}

	if (!(Test-Path $platformKeyPath)){
		New-Item -Path HKCU:SOFTWARE -Name ParticularSoftware | Out-Null
	}

	Set-ItemProperty -Path $platformKeyPath -Name "NuGetUser" -Value "true" | Out-Null

} 
Catch [Exception] { 
	Write-Warning $error[0]
}

$url = "http://particular.net/download-the-particular-service-platform?version=$packageVersion" 
$url = $url.ToLowerInvariant(); 

if($dte){
	$dte.ExecuteCommand("View.URL", $url)
}
else{
	"No dte detected, url: $url"
}
