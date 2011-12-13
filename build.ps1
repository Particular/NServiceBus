Import-Module .\tools\psake\psake.psm1
Invoke-psake .\default.ps1 PrepareBinaries
Remove-Module psake