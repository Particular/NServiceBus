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
$nservicebusVersion = Get-NServiceBusVersion
$nserviceBusVersionPath =  $nserviceBusKeyPath +  "\" + $nservicebusVersion.Major + "." + $nservicebusVersion.Minor

#Figure out if this machine is properly setup
$nsbVersionDetails = Get-ItemProperty -path $nserviceBusVersionPath -ErrorAction silentlycontinue
if (($nsbVersionDetails -ne $null) -and ($nsbVersionDetails.Length -ne 0)){
$preparedInVersion  = $nsbVersionDetails.psobject.properties | ?{ $_.Name -eq $machinePreparedKey }
$dontCheckMachineSetup  = $nsbVersionDetails.psobject.properties | ?{ $_.Name -eq "DontCheckMachineSetup" }

if($preparedInVersion.value){
	$machinePrepared = $true
}
  
if($machinePrepared -or $dontCheckMachineSetup.value)
{
	exit
}
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

if (($nsbVersionDetails -ne $null) -and ($nsbVersionDetails.Length -ne 0)){
New-Item -Path $nserviceBusVersionPath | Out-Null
New-ItemProperty -Path $nserviceBusVersionPath -Name $machinePreparedKey -PropertyType String -Value "true" | Out-Null
}

$url = "http://particular.net/articles/preparing-your-machine-to-run-nservicebus?dtc=" + $dtcInstalled + "&msmq=" + $msmqInstalled + "&raven=" + $ravenDBInstalled + "&perfcounter=" + $perfCountersInstalled + "&installer=NServiceBus&method=nuget&version=" + $package.Version
$url = $url.ToLowerInvariant(); 

$dte.ExecuteCommand("View.URL", $url)