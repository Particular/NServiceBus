$packageIds = $args

dir -recurse -include ('packages.config') |ForEach-Object {
	$packageconfig = [io.path]::Combine($_.directory,$_.name)

	write-host $packageconfig

	if($packageIds -ne "")
	{
		write-host "Doing an unsafe update of" $packageIds 
		.\tools\NuGet\NuGet.exe update $packageconfig -RepositoryPath packages -Id $packageIds
	}
	else
	{	
		write-host "Doing a safe update of all packages" $packageIds 
		.\tools\NuGet\NuGet.exe update -Safe $packageconfig -RepositoryPath packages
	}
}

