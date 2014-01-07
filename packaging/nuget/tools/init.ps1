param($installPath, $toolsPath, $package, $project)

#default to build dir when debugging
if(!$toolsPath){
	$toolsPath = ""
}

if (Get-Module NServiceBus.Powershell) {
	Remove-Module NServiceBus.Powershell
}

Import-Module (Join-Path $toolsPath NServiceBus.Powershell.dll)

Write-Host ""
Write-Host "Type 'get-help about_NServiceBus' to see all available NServiceBus commands."
Write-Host ""

$nserviceBusKeyPath =  "HKCU:SOFTWARE\NServiceBus" 
$machinePreparedKey = "MachinePrepared"
$machinePrepared = $false
$versionParts = $package.Version.ToString().Split('.')
$nservicebusVersion = $versionParts[0]

if($versionParts.Length -gt 1) {
    $nservicebusVersion += "." + $versionParts[1]
}

$nserviceBusVersionPath =  $nserviceBusKeyPath +  "\" + $nservicebusVersion

#Figure out if this machine is properly setup
try {
	if (!(Test-Path $nserviceBusKeyPath)) {
		New-Item -Path HKCU:SOFTWARE -Name NServiceBus | Out-Null
	}

	if (!(Test-Path $nserviceBusVersionPath)){
		$versionToAdd = $nservicebusVersion
		New-Item -Path $nserviceBusKeyPath -Name $versionToAdd | Out-Null
		New-ItemProperty -Path $nserviceBusVersionPath -Name $machinePreparedKey -PropertyType String -Value "false" | Out-Null
	}
	else
	{
		$a = Get-ItemProperty -path $nserviceBusVersionPath
		$preparedInVersion  = $a.psobject.properties | ?{ $_.Name -eq $machinePreparedKey }
		$dontCheckMachineSetup  = $a.psobject.properties | ?{ $_.Name -eq "DontCheckMachineSetup" }

		if($preparedInVersion.value -eq $true){
			$machinePrepared = $true
		}
	  
		if($machinePrepared -or $dontCheckMachineSetup.value)
		{
			exit
		}
	}
} Catch [Exception] { 
	Write-Warning "There was a problem checking if this machine is properly setup:"	
	Write-Warning $error[0]
}

$perfCountersInstalled = $false
$msmqInstalled = $false
$dtcInstalled = $false
$ravenDBInstalled = $false

try {
	$perfCountersInstalled = Test-NServiceBusPerformanceCountersInstallation
} Catch [System.Security.SecurityException] { }
try {
	$msmqInstalled = Test-NServiceBusMSMQInstallation
} Catch [System.Security.SecurityException] { }
try {
	$dtcInstalled = Test-NServiceBusDTCInstallation
} Catch [System.Security.SecurityException] { }
try {
	$ravenDBInstalled = Test-NServiceBusRavenDBInstallation
	if(!$ravenDBInstalled) {
		$ravenDBInstalled = Test-NServiceBusRavenDBInstallation -Port 8081
	}
} Catch [System.Security.SecurityException] { }

if(!$perfCountersInstalled){
	Write-Warning "Performance counters are not installed."
}
if(!$msmqInstalled){
	Write-Warning "Msmq is not installed or correctly setup."
}
if(!$dtcInstalled){
	Write-Warning "DTC is not installed or started."
}
if(!$ravenDBInstalled){
	Write-Warning "RavenDB is not installed. We checked port 8080 and 8081."
}

if($perfCountersInstalled -and $msmqInstalled -and $dtcInstalled -and $ravenDBInstalled){
	Write-Host "Required infrastructure is all setup correctly."
}

try {
	Set-ItemProperty -Path $nserviceBusVersionPath -Name $machinePreparedKey -Value "true" | Out-Null
} Catch [Exception] { }


$url = "http://particular.net/articles/preparing-your-machine-to-run-nservicebus?dtc=" + $dtcInstalled + "&msmq=" + $msmqInstalled + "&raven=" + $ravenDBInstalled + "&perfcounter=" + $perfCountersInstalled + "&installer=NServiceBus&method=nuget&version=" + $package.Version
$url = $url.ToLowerInvariant(); 

$dte.ExecuteCommand("View.URL", $url)
