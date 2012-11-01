$scriptPath = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
Get-ChildItem -Filter nuget.exe -Recurse -Path $scriptPath | ForEach-Object {
    Write-Host Checking $_.FullName
    &$_.FullName update -Self
    if(Test-Path ($_.FullName + ".old")) {
		Remove-Item -Path ($_.FullName + ".old")
    }
}