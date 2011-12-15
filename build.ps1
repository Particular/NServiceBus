Import-Module .\tools\psake\psake.psm1 -ErrorAction SilentlyContinue
if($args -ne $null){
Invoke-psake .\default.ps1 $args
}
else{
Invoke-psake .\default.ps1 PrepareBinaries
}
Remove-Module psake -ErrorAction SilentlyContinue