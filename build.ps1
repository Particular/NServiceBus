Import-Module .\tools\psake\psake.psm1
Invoke-Psake InstallDependentPackages;
Invoke-Psake GeneateCommonAssemblyInfo;
Remove-Module psake