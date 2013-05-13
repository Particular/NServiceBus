[Reflection.Assembly]::LoadWithPartialName("System.Messaging")

[System.Messaging.MessageQueue]::GetPrivateQueuesByMachine("localhost") | % {".\" + $_.QueueName} | % {[System.Messaging.MessageQueue]::Delete($_); }