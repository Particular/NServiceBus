param(
    [Parameter(Position=1, Mandatory=0)]
    [switch]$genAsmInfo = $true
  )


Import-Module .\tools\psake\psake.psm1 -ErrorAction SilentlyContinue
Invoke-psake .\default.ps1 -taskList @("CreatePackages")  -properties @{BuildNumber="4"; PreRelease="-local"}
Remove-Module psake -ErrorAction SilentlyContinue