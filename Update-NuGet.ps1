$scriptPath = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
Get-ChildItem -Filter nuget.exe -Name -Recurse -Path $scriptPath | ForEach-Object {
	$nugetExePath = Join-Path $scriptPath $_
    echo "Checking $_"
    Start-Process -NoNewWindow -Wait -FilePath $nugetExePath -ArgumentList 'update -Self'
    if(Test-Path "${nugetExePath}.old") {
		Remove-Item -Path "${nugetExePath}.old"
    }
}