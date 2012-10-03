Import-Module ..\bin\debug\nservicebus.powershell.dll

Get-Help Install-PerformanceCounters

#Checks the status of the NServiceBus PerformanceCounters on this box
$countersIsGood = Install-PerformanceCounters -WhatIf
"PerformanceCounters is good: " + $countersIsGood

#Setup the NServiceBus perfcounters
$countersIsGood = Install-PerformanceCounters
"PerformanceCounters is good: " + $countersIsGood