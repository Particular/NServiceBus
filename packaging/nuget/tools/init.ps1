param($installPath, $toolsPath, $package, $project)

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
		$isNewUser = $false
	}
	
	if (Test-Path $platformKeyPath){
		$isNewUser = $false
	}

	if (!($isNewuser)) {
		exit
	}

	if (!(Test-Path $platformKeyPath)){
		New-Item -Path HKCU:SOFTWARE -Name ParticularSoftware | Out-Null
	}

	Set-ItemProperty -Path $platformKeyPath -Name "NuGetUser" -Value "true" | Out-Null


    $url = "http://particular.net/download-the-particular-service-platform?version=$packageVersion" 
    $url = $url.ToLowerInvariant(); 

    if($dte){
	    $dte.ExecuteCommand("View.URL", $url)
    }
} 
Catch [Exception] { 
	Write-Warning $error[0]
}
