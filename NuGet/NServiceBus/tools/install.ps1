param($installPath, $toolsPath, $package, $project)

#default to build dir when debugging
if(!$toolsPath){
    $toolsPath = $pwd.path + "\..\..\..\build\nservicebus.core\"
}

$nserviceBusKeyPath =  "HKCU:SOFTWARE\NServiceBus" 

$nserviceBusKey = get-itemproperty -path $nserviceBusKeyPath -ErrorAction silentlycontinue
$dontSuggestGettingStarted  = $nserviceBusKey.psobject.properties | ?{ $_.Name -eq "DontSuggestGettingStarted" }
 
if($dontSuggestGettingStarted.value)
{
    exit
}

$formsPath = Join-Path $toolsPath nservicebus.forms.dll
[void] [System.Reflection.Assembly]::LoadFile($formsPath) 

$firstTimeUserDialog = New-Object NServiceBus.Forms.FirstTimeUser

[void] $firstTimeUserDialog.ShowDialog()

if($firstTimeUserDialog.OpenGettingStartedGuide){
    start 'http://nservicebus.com/GettingStarted/NuGet'
}

if($firstTimeUserDialog.DontBotherMeAgain){

    New-Item -Path $nserviceBusKeyPath -Force
    New-ItemProperty -Path $nserviceBusKeyPath -Name "DontSuggestGettingStarted" -PropertyType String -Value "true" -Force
}

