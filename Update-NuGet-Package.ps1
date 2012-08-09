$ids = $args
$scriptPath = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
Get-ChildItem -Filter packages.config -Name -Recurse -Path $scriptPath | ForEach-Object {
	$nugetExePath = Join-Path $scriptPath 'tools\NuGet\nuget.exe'
    $packagePath = Join-Path $scriptPath $_
    $nugetRepositoryPath = Join-Path $scriptPath 'packages'
    
    echo "Checking if $packagePath contains ""$ids"" package(s) and updating if required..."
    
    $arguments = "update ""$packagePath"" -RepositoryPath ""$nugetRepositoryPath"" -Id ""$ids"""
       
    Start-Process -NoNewWindow -Wait -FilePath $nugetExePath -ArgumentList $arguments
    
}