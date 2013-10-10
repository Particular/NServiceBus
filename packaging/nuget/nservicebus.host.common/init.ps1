param($rootPath, $toolsPath, $package, $project)

if (Get-Module T4Scaffolding) {
	# Disable scaffolding as much as possible until VS is restarted
	Remove-Module T4Scaffolding
}

# OK, we've got the correct .NET assembly version or none at all (in which case we can load the correct version)
$dllPath = Join-Path $toolsPath T4Scaffolding.dll
$packagesRoot = [System.IO.Path]::GetDirectoryName($rootPath)

if (Test-Path $dllPath) {
	# Load the .NET PowerShell module and set up aliases, tab expansions, etc.
	Import-Module $dllPath
	[T4Scaffolding.NuGetServices.Services.ScaffoldingPackagePathResolver]::SetPackagesRootDirectory($packagesRoot)
	Set-Alias Scaffold Invoke-Scaffolder -Option AllScope -scope Global
} 
else 
{
	Write-Warning ("Could not find T4Scaffolding module. Looked for: " + $dllPath)
}
