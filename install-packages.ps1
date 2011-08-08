dir -recurse -include ('packages.config') |ForEach-Object {
	$packageconfig = [io.path]::Combine($_.directory,$_.name)

	write-host $packageconfig 

	.\tools\NuGet\NuGet.exe install $packageconfig -o packages 
}

