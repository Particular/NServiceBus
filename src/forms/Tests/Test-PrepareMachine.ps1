$path = "C:\dev\NServiceBus\src\forms\bin\Debug\nservicebus.forms.dll"

[void] [System.Reflection.Assembly]::LoadFile($path) 

$form = New-Object NServiceBus.Forms.PrepareMachine
#$form.Add_Shown({$form.Activate()})
[void] $form.ShowDialog()

"Allow prepare: " + $form.AllowPrepare
