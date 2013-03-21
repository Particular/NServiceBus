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
$a = Get-ItemProperty -path $nserviceBusVersionPath -ErrorAction silentlycontinue
$preparedInVersion  = $a.psobject.properties | ?{ $_.Name -eq $machinePreparedKey }
$dontCheckMachineSetup  = $a.psobject.properties | ?{ $_.Name -eq "DontCheckMachineSetup" }

if($preparedInVersion.value){
	$machinePrepared = $true
}
  
if($machinePrepared -or $dontCheckMachineSetup.value)
{
	exit
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
	Write-Warning "RavenDB is not installed."
}

if($perfCountersInstalled -and $msmqInstalled -and $dtcInstalled -and $ravenDBInstalled){
	Write-Host "Required infrastructure is all setup correctly."
}

New-Item -Path $nserviceBusVersionPath -ErrorAction silentlycontinue | Out-Null
New-ItemProperty -Path $nserviceBusVersionPath -Name $machinePreparedKey -PropertyType String -Value "true" | Out-Null

$dte.ExecuteCommand("View.URL", "http://www.nservicebus.com/RequiredInfrastructure/Windows/Setup?dtc=" + $dtcInstalled + "&msmq=" + $msmqInstalled + "&raven=" + $ravenDBInstalled + "&perfcounter=" + $perfCountersInstalled+ "&version=" + $nservicebusVersion)