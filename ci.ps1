param([Int32]$patch)

Import-Module .\tools\psake\psake.psm1
Invoke-psake .\default.ps1 -taskList @("PrepareRelease","CreatePackages") -properties @{ProductVersion="3.3";PatchVersion="$patch";PreRelease="";buildConfiguration="Release"}
Remove-Module psake
exit $LASTEXITCODE