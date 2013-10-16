Import-Module ..\bin\debug\nservicebus.powershell.dll

Get-Help Install-Dtc 

#Checks the status of the DTC on this box
$dtcIsGood = Install-Dtc -WhatIf
"DTC is good: " + $dtcIsGood

#Configure the DTC for nservicebus use
#$dtcIsGood = Install-Dtc
"DTC is good: " + $dtcIsGood