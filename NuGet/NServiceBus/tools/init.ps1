param($installPath, $toolsPath, $package, $project)

#default to build dir when debugging
if(!$toolsPath){
	$toolsPath = "..\..\NServiceBus.PowerShell\bin\Debug"
}

Import-Module (Join-Path $toolsPath nservicebus.powershell.dll)

Write-Host
Write-Host "Type 'get-help about_NServiceBus' to see all available NServiceBus commands."
Write-Host

$nserviceBusKeyPath =  "HKCU:SOFTWARE\NServiceBus" 
$machinePreparedKey = "MachinePrepared"
$machinePrepared = $false
$nservicebusVersion = Get-NServiceBusVersion
$nserviceBusVersionPath =  $nserviceBusKeyPath +  "\" + $nservicebusVersion.Major + "." + $nservicebusVersion.Minor

#Figure out if this machine is properly setup
$a = get-itemproperty -path $nserviceBusVersionPath -ErrorAction silentlycontinue
$preparedInVersion  = $a.psobject.properties | ?{ $_.Name -eq $machinePreparedKey }
$dontCheckMachineSetup  = $a.psobject.properties | ?{ $_.Name -eq "DontCheckMachineSetup" }

if($preparedInVersion.value){
	$machinePrepared = $true
}
  
if($machinePrepared -or $dontCheckMachineSetup.value)
{
	exit
}

$perfCountersInstalled = Test-NServiceBusPerformanceCountersInstallation
$msmqInstalled = Test-NServiceBusMSMQInstallation
$dtcInstalled = Test-NServiceBusDTCInstallation
$ravenDBInstalled = Test-NServiceBusRavenDBInstallation

if(!$perfCountersInstalled){
	Write-Host "Performance counters are not installed."
}
if(!$msmqInstalled){
	Write-Host "Msmq is not installed or correctly setup."
}
if(!$dtcInstalled){
	Write-Host "DTC is not installed or started."
}
if(!$ravenDBInstalled){
	Write-Host "RavenDB is not installed."
}

if($perfCountersInstalled -and $msmqInstalled -and $dtcInstalled -and $ravenDBInstalled){
	Write-Host "Required infrastructure is all setup correctly."

	New-Item -Path $nserviceBusVersionPath -Force | Out-Null
	New-ItemProperty -Path $nserviceBusVersionPath -Name $machinePreparedKey -PropertyType String -Value "true" -Force | Out-Null
	exit
}

$dte.ExecuteCommand("View.URL", "http://www.nservicebus.com/RequiredInfrastructure/Windows/Setup?dtc=" + $dtcInstalled + "&msmq=" + $msmqInstalled + "&raven=" + $ravenDBInstalled + "&perfcounter=" + $perfCountersInstalled)