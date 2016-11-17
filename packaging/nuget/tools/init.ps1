param($installPath, $toolsPath, $package, $project)

$packageVersion = 'Unknown'
$noticeEvent = 'noticeEvent'
$jobName = 'analytics'

# cleanup previous runs
Get-Job | ? { $_.Name  -eq $jobName } | Remove-Job -Force -ErrorAction SilentlyContinue    
 
if ($package) {
    $packageVersion = $package.Version
}

# Define script to run in background job
$jobScriptBlock = { 
    param($packageVersion, $noticeEvent)

    # Set Tracing on within the Job
    Set-PSDebug -Trace 2

    # Setup event forwarding to foreground
    Register-EngineEvent -SourceIdentifier $noticeEvent -Forward 
    
    $nserviceBusKeyPath = 'HKCU:SOFTWARE\NServiceBus' 
    $platformKeyPath = 'HKCU:SOFTWARE\ParticularSoftware'
    
    if ((Test-Path $nserviceBusKeyPath) -or (Test-Path $platformKeyPath)) {
        New-Event -SourceIdentifier $noticeEvent -Sender "analytics" -MessageData "existing"
    }
    else {
        New-Event -SourceIdentifier $noticeEvent -Sender "analytics" -MessageData "newuser"

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

$notice = @" 
Reporting first time usage and version information to www.particular.net. 
This call does not collect any personal information. For more details, 
see the License Agreement and the Privacy Policy available here: https://particular.net/licenseagreement.
"@

# Run JobScript
$job = Start-Job -ScriptBlock $jobScriptBlock -Name $jobName -ArgumentList $packageVersion, $noticeEvent 

# Wait for show notice event
$event = Wait-Event -SourceIdentifier $noticeEvent -Timeout 5 
Remove-Event -SourceIdentifier $noticeEvent -ErrorAction SilentlyContinue  
if ($event.MessageData -eq "newuser") {
    Write-Host $notice
}
