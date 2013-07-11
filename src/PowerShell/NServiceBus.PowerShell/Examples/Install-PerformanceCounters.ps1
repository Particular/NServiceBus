Import-Module ..\..\..\..\binaries\nservicebus.powershell.dll

Get-Help Install-NServiceBusPerformanceCounters

#Checks the status of the NServiceBus PerformanceCounters on this box
#$countersIsGood = Install-NServiceBusPerformanceCounters -WhatIf
#"PerformanceCounters is good: " + $countersIsGood

#Setup the NServiceBus perfcounters
$countersIsGood = Install-NServiceBusPerformanceCounters
"PerformanceCounters is good: " + $countersIsGood