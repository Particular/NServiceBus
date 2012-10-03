param($installPath, $toolsPath, $package, $project)

#default to build dir when debugging
if(!$toolsPath){
	$toolsPath = "..\..\..\build\nservicebus.core\"
}

#Import the nservicebus ps-commandlets
Import-Module (Join-Path $toolsPath nservicebus.powershell.dll)


$nserviceBusKeyPath =  "HKCU:SOFTWARE\NServiceBus" 
$machinePrepared = $false

#Figure out if this machine is properly setup
$a = get-itemproperty -path $nserviceBusKeyPath -ErrorAction silentlycontinue
$preparedInVersion  = $a.psobject.properties | ?{ $_.Name -eq "MachinePreparedByVersion" }
$dontCheckMachineSetup  = $a.psobject.properties | ?{ $_.Name -eq "DontCheckMachineSetup" }

if($preparedInVersion.value){
	$machinePrepared = $true
}
  
if($machinePrepared -or $dontCheckMachineSetup.value)
{
	exit
}

$perfCountersInstalled = Install-PerformanceCounters -WhatIf
$msmqInstalled = Install-Msmq -WhatIf
$dtcInstalled = Install-Dtc -WhatIf
$ravenDBInstalled = Install-RavenDB -WhatIf

if(!$perfCountersInstalled){
	"Performance counters needs to be setup"
}

if(!$msmqInstalled){
	"Msmq needs to be setup"
}
if(!$dtcInstalled){
	"DTC needs to be setup"
}
if(!$ravenDBInstalled){
	"RavenDB needs to be setup"
}

if($perfCountersInstalled -and $msmqInstalled -and $dtcInstalled -and $ravenDBInstalled){
	"Required infrastructure is all setup no need to continue"

	#todo - set the machine is prepared flag in the registry
	exit
}

[void] [System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") 
[void] [System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") 

$form = New-Object System.Windows.Forms.Form 
$form.Text = "Do you need help preparing your machine for NServiceBus?"
$form.Size = New-Object System.Drawing.Size(400,300) 
#$form.BackColor = New-Object  System.Drawing.Color.White
$form.StartPosition = "CenterScreen"
#$form.FormBorderStyle = New-Object System.Windows.Forms.FormBorderStyle.FixedDialog

$form.KeyPreview = $True
$form.Add_KeyDown({if ($_.KeyCode -eq "Enter") 
    {$x=$objTextBox.Text;$form.Close()}})
$form.Add_KeyDown({if ($_.KeyCode -eq "Escape") 
    {$form.Close()}})

$OKButton = New-Object System.Windows.Forms.Button
$OKButton.Location = New-Object System.Drawing.Size(20,150)
$OKButton.Size = New-Object System.Drawing.Size(360,23)
$OKButton.Text = "Let NServiceBus set everything up for me"
$OKButton.Add_Click({$autoSetup=$true;$form.Close()})
$form.Controls.Add($OKButton)

$CancelButton = New-Object System.Windows.Forms.Button
$CancelButton.Location = New-Object System.Drawing.Size(20,180)
$CancelButton.Size = New-Object System.Drawing.Size(360,23)
$CancelButton.Text = "Tell me more about the infrastructure required by NServiceBus"
$CancelButton.Add_Click({$tellMeMore=$true;$form.Close()})
$form.Controls.Add($CancelButton)

$label = New-Object System.Windows.Forms.Label
$label.Location = New-Object System.Drawing.Size(20,20) 
$label.Size = New-Object System.Drawing.Size(360,60) 
$label.Text = "We couldn't detect that this machine has been setup for use with NServiceBus."
$form.Controls.Add($label) 

$dontBotherMeAgainCheckbox = New-Object System.Windows.Forms.CheckBox
$dontBotherMeAgainCheckbox.Location = New-Object System.Drawing.Size(20,220) 
$dontBotherMeAgainCheckbox.Size = New-Object System.Drawing.Size(360,23) 
$dontBotherMeAgainCheckbox.Text = "Don't bother me again"
$dontBotherMeAgainCheckbox.Add_CheckedChanged({$dontBotherMeAgain=$dontBotherMeAgainCheckbox.Checked})

$form.Controls.Add($dontBotherMeAgainCheckbox) 

$form.Topmost = $True

$form.Add_Shown({$form.Activate()})
[void] $form.ShowDialog()

if($tellMeMore){
	start 'http://nservicebus.com/RequiredInfrastructure'
}

if($autoSetup){
	if(!$perfCountersInstalled){
		Install-PerformanceCounters
	}
	if(!$msmqInstalled){
		$success = Install-Msmq
		if(!$success){
			#TODO - ask users if we should force a update
			if($false){
				Install-Msmq -Force
			}
		}
	}
	if(!$dtcInstalled){
		Install-Dtc
	}
	if(!$ravenDBInstalled){
		Install-RavenDB
	}
}

if($dontBotherMeAgain){

	New-Item -Path $nserviceBusKeyPath -Force
	New-ItemProperty -Path $nserviceBusKeyPath -Name "DontCheckMachineSetup" -PropertyType String -Value "true" -Force
}
	