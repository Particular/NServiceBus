$ids = $args
$scriptPath = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
Get-ChildItem -Filter packages.config -Recurse -Path $scriptPath | ForEach-Object {
	$nugetExePath = Join-Path $scriptPath 'tools\NuGet\nuget.exe'
    $packagePath = $_.FullName
    $nugetRepositoryPath = Join-Path $scriptPath 'packages'
    
    Write-Host Checking if $packagePath contains "$ids" package and updating if required...
       
    &$nugetExePath update $_.FullName -RepositoryPath $nugetRepositoryPath -Id $ids
}