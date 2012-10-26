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


$dte.ExecuteCommand("View.URL", "http://www.nservicebus.com/GettingStarted/NuGet")

New-Item -Path $nserviceBusKeyPath -Force
New-ItemProperty -Path $nserviceBusKeyPath -Name "DontSuggestGettingStarted" -PropertyType String -Value "true" -Force

