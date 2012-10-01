param($installPath, $toolsPath, $package, $project)

#Import the nservicebus ps-commandlets
#if($installPath){
#	Import-Module (Join-Path $installPath nservicebus.dll)
#}

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


if($dontBotherMeAgain){

	New-Item -Path $nserviceBusKeyPath -Force
	New-ItemProperty -Path $nserviceBusKeyPath -Name "DontCheckMachineSetup" -PropertyType String -Value "true" -Force
}

if($tellMeMore){
	start 'http://nservicebus.com/RequiredInfrastructure'
}

if($autoSetup){
	# TODO - enable this when the cmd-let is done
	#Install-Infrastructure 
}
	