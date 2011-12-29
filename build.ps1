param(
    [Parameter(Position=0,Mandatory=0)]
    [string[]]$taskList = @(),
    [Parameter(Position=1, Mandatory=0)]
    [System.Collections.Hashtable]$properties = @{},
	[Parameter(Position=2, Mandatory=0)]
    [switch]$genAsmInfo = $false,
	[Parameter(Position=3, Mandatory=0)]
    [switch]$desc = $false
  )

if(($taskList -eq $null) -or ($args -eq $null)){
	$taskList = @("PrepareBinaries")
}
elseif($taskList.Count -le 0){
	$taskList = @("PrepareBinaries")
}


Import-Module .\tools\psake\psake.psm1 -ErrorAction SilentlyContinue

if($desc){
	Invoke-psake .\default.ps1 -docs
}
else{

	Invoke-psake .\default.ps1 -taskList $taskList  -properties $properties
}
Remove-Module psake -ErrorAction SilentlyContinue