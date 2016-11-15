param($installPath, $toolsPath, $package, $project)

$packageVersion = 'Unknown'

$notice = @"
For first time users this script collects install and version information. 
    
This call does not collect any personal information. For more details, see the 
License Agreement and the Privacy Policy available here: http://particular.net/licenseagreement.
"@

if ($package) {
    $packageVersion = $package.Version
}

# Define script to run in background job
$jobScriptBlock = { 
    param($packageVersion)

    # Set Tracing on with the Job
    Set-PSDebug -Trace 2

    $nserviceBusKeyPath = 'HKCU:SOFTWARE\NServiceBus' 
    $platformKeyPath = 'HKCU:SOFTWARE\ParticularSoftware'
    
    # Test for Existing reg keys and skip if either are found
    if (-not ((Test-Path $nserviceBusKeyPath) -or (Test-Path $platformKeyPath))) {
    
        # Set Flag to bypass first time user feedback in Platform Installer
        New-Item -Path $platformKeyPath -Force | Out-Null
        Set-ItemProperty -Path $platformKeyPath -Name 'NuGetUser' -Value 'true' -Force
    
        # Post Version to particular.net
        $wc = New-Object System.Net.WebClient 
        try {
            $url = 'https://particular.net/api/ReportFirstTimeInstall'
            $postData  = New-Object System.Collections.Specialized.NameValueCollection
            $postData.Add("version", $packageversion)
            $wc.UseDefaultCredentials = $true
            $wc.UploadValues($url, "post", $postdata)
        } 
        finally {
            # Dispose
            Remove-Variable -Name wc 
        }
    }
}

# If an existing instance of this job exists then we can skip running it, otherwise create and run 
$jobName = 'particular.analytics'
$job = Get-Job -Name  $jobName -ErrorAction SilentlyContinue
if (-not $job) {
    Write-Output $notice
    $job = Start-Job -ScriptBlock $jobScriptBlock -Name $jobName -ArgumentList $packageVersion 
}