param(
	[Parameter(Position=1, Mandatory=0)]
    $buildNumber = 0,
	
    [Parameter(Position=2, Mandatory=0)]
    [switch]$genAsmInfo = $true
)


Import-Module .\tools\psake\psake.psm1 -ErrorAction SilentlyContinue

# You should increment the BuildNumber whereever you used/referenced the locally built packages
# Point your VS Package Manager / Nuget at ./release/packages or ./core-only/packages to use your locally built package

Invoke-psake .\default.ps1 -taskList @("CreatePackages")  -properties @{BuildNumber=$buildNumber; PreRelease="-local"}
Remove-Module psake -ErrorAction SilentlyContinue
