param($installPath, $toolsPath, $package, $project)

$nserviceBusKeyPath =  "HKCU:SOFTWARE\NServiceBus" 
$machinePrepared = $false

$nserviceBusKey = get-itemproperty -path $nserviceBusKeyPath -ErrorAction silentlycontinue
$dontSuggestGettingStarted  = $nserviceBusKey.psobject.properties | ?{ $_.Name -eq "DontSuggestGettingStarted" }

if($preparedInVersion.value){
    $machinePrepared = $true
}
  
if($dontSuggestGettingStarted.value)
{
    exit
}

[void] [System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") 
[void] [System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") 

$form = New-Object System.Windows.Forms.Form 
$form.Text = "First time user?"
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
$OKButton.Text = "Take me to the getting started guide"
$OKButton.Add_Click({$openGettingStarted=$true;$form.Close()})
$form.Controls.Add($OKButton)

$CancelButton = New-Object System.Windows.Forms.Button
$CancelButton.Location = New-Object System.Drawing.Size(20,180)
$CancelButton.Size = New-Object System.Drawing.Size(360,23)
$CancelButton.Text = "I know enough to get started on my own"
$CancelButton.Add_Click({$form.Close()})
$form.Controls.Add($CancelButton)

$label = New-Object System.Windows.Forms.Label
$label.Location = New-Object System.Drawing.Size(20,20) 
$label.Size = New-Object System.Drawing.Size(360,60) 
$label.Text = "If this is your first time using NServiceBus, we suggest going through our Getting Started guide first"
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
    New-ItemProperty -Path $nserviceBusKeyPath -Name "DontSuggestGettingStarted" -PropertyType String -Value "true" -Force
}

if($openGettingStarted){
    start 'http://nservicebus.com/GettingStarted/NuGet'
}
