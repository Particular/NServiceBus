Import-Module ..\bin\debug\nservicebus.powershell.dll

Get-Help Install-Msmq 

#Checks the status of Msmq on this box
$msmqIsGood = Install-Msmq -WhatIf
"Msmq is good: " + $msmqIsGood

#Installs and starts msmq if not installed already
$msmqIsGood = Install-Msmq 
"Msmq is good: " + $msmqIsGood
#Allows us to reinstall (with loss of data) msmq if needed
$msmqIsGood = Install-Msmq -Force
"Msmq is good: " + $msmqIsGood