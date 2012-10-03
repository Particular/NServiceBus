Import-Module ..\bin\debug\nservicebus.powershell.dll

Remove-Item c:\temp\nservicebus.persistence -recurse -force

Get-Help Install-RavenDB

$ravenIsGood = Install-RavenDB -WhatIf -installpath c:\temp\nservicebus.persistence
"RavenDB is good: " + $ravenIsGood 

Install-RavenDB -port 8888 -installpath c:\temp\nservicebus.persistence