Import-Module .\tools\psake\psake.psm1
Invoke-Psake CreatePackages;
Invoke-Psake ZipOutput
Invoke-Psake FinalizeAndClean
Remove-Module psake