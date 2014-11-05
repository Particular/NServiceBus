properties {
	$ProductVersion = "4.0"
	$PatchVersion = "0"
	$BuildNumber = if($env:BUILD_NUMBER -ne $null) { $env:BUILD_NUMBER } else { "0" }
	$PreRelease = "alpha"
	$NugetKey = ""
	$UploadPackage = $false
}

$baseDir = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
$toolsDir = "$baseDir\tools"
$nugetExec = "$toolsDir\NuGet\NuGet.exe"
$packageOutPutDir = "$baseDir\nugets"
$chocoPackageOutPutDir = "$baseDir\chocos"
$nugetTempPath = "NugetTemp"

task default -depends Build

task Clean { 
	if ( -Not (Test-Path $packageOutPutDir))
	{
		New-Item $packageOutPutDir -ItemType Directory | Out-Null
	} else {
		Remove-Item ($packageOutPutDir + '\*.nupkg')
	}
	if ( -Not (Test-Path $chocoPackageOutPutDir))
	{
		New-Item $chocoPackageOutPutDir -ItemType Directory | Out-Null
	} else {
		Remove-Item ($chocoPackageOutPutDir + '\*.nupkg')
	}
}

task Pack {
	(dir -Path $nugetTempPath -Recurse -Filter '*.nuspec') | foreach { 
			   
			Write-Host Creating NuGet spec file for $_.Name
			
			[xml] $nuspec = Get-Content $_.FullName
			
			$nugetVersion = $ProductVersion + "." + $PatchVersion
			
			if($PreRelease -ne '') {
				$nuspec.package.metadata.title += ' (' + $PreRelease + ')'
				$nugetVersion = $ProductVersion + "." + $PatchVersion + "-" + $PreRelease + $BuildNumber 
			}
	
			$nuspec.package.metadata.version = $nugetVersion
			
			$nuspec | Select-Xml '//dependency[starts-with(@id, "NServiceBus")]' |% {
				$_.Node.version = "[$nugetVersion]"
			}
			$nuspec | Select-Xml '//file[starts-with(@src, "\")]' |% {
				$_.Node.src = $baseDir + $_.Node.src
			}
			
			if(Test-Path -Path ($_.DirectoryName + '\content')){
				$fileElement = $nuspec.CreateElement('file')
				$fileElement.SetAttribute('src', $_.DirectoryName + '\content\**')
				$fileElement.SetAttribute('target', 'content')
				$nuspec.package.files.AppendChild($fileElement) > $null
			}
			
			if(Test-Path -Path ($_.DirectoryName + '\tools')){
				$fileElement = $nuspec.CreateElement('file')
				$fileElement.SetAttribute('src', $_.DirectoryName + '\tools\**')
				$fileElement.SetAttribute('target', 'tools')
				$nuspec.package.files.AppendChild($fileElement) > $null
			}
			
			$nuspec.Save($_.FullName);
			
			&$nugetExec pack $_.FullName -OutputDirectory $packageOutPutDir
		}
}

task Push {
	if(($UploadPackage) -and ($NugetKey -eq "")){
		throw "Could not find the NuGet access key Package Cannot be uploaded without access key"
	}
	if($UploadPackage) {
		(dir -Path $packageOutPutDir -Filter '*.nupkg') | foreach { 
			&$nugetExec push $_.FullName $NugetKey
		}
	}
}

task Init {
	copy 'Nuget\' $nugetTempPath -Recurse -Force
}

task Build -depends Clean, Init, Pack, Push {
	del $nugetTempPath -Recurse -Force -ErrorAction SilentlyContinue
}