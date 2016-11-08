param($installPath, $toolsPath, $package, $project)

$nserviceBusKeyPath =  "HKCU:SOFTWARE\NServiceBus" 

$platformKeyPath = "HKCU:SOFTWARE\ParticularSoftware"
$isNewUser = $true

$packageVersion = "Unknown"

if($package){
	$packageVersion = $package.Version
}

#Figure out if this is a first time user
try {

	#Check for existing NServiceBus installations
	if (Test-Path $nserviceBusKeyPath) {
		$isNewUser = $false
	}
	
	if (Test-Path $platformKeyPath){
		$isNewUser = $false
	}

	if (!($isNewuser)) {
		exit
	}

	if (!(Test-Path $platformKeyPath)){
		New-Item -Path HKCU:SOFTWARE -Name ParticularSoftware | Out-Null
	}

	Set-ItemProperty -Path $platformKeyPath -Name "NuGetUser" -Value "true" | Out-Null

	Write-Verbose 'Reporting first time install and version information to www.particular.net. This call does not collect any personal information. For more details, see the License Agreement and the Privacy Policy available here: http://particular.net/licenseagreement. Subsequent NuGet installs or updates will not invoke this call.' -verbose
	$url = 'https://particular.net/api/ReportFirstTimeUsage'
	$postData  = New-Object System.Collections.Specialized.NameValueCollection
	$postData.Add("version", $packageversion)
	$wc = New-Object System.Net.WebClient
	$wc.UseDefaultCredentials = $true
	$wc.UploadValuesAsync($url,"post", $postdata)
} 
Catch [Exception] { 
	Write-Warning $error[0]
}
finally {
	if ($wc){
	$wc.Dispose()
	}
}
