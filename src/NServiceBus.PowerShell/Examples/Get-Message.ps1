Import-Module ..\bin\debug\nservicebus.powershell.dll


Get-Message -QueueName result | select -ExpandProperty Headers | Where-Object{$_.Key -eq "NServiceBus.RelatedToTimeoutId" } | group Value | where Count -gt 1
