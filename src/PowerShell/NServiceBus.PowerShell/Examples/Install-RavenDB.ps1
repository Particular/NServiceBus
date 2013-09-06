Import-Module ..\bin\debug\nservicebus.powershell.dll

Remove-Item c:\temp\nservicebus.persistence -recurse -force

Get-Help Install-RavenDB

$ravenIsGood = Install-RavenDB -WhatIf -Path c:\temp\nservicebus.persistence
"RavenDB is good: " + $ravenIsGood 

Install-RavenDB -Port 8888 -Path c:\MyPath\Nservicebus.Persistence