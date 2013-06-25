properties {
	$ProductVersion = "4.0"
	$PatchVersion = "0"
	$BuildNumber = "0"
	$PreRelease = "alpha"
	$NugetKey = ""
	$UploadPackage = $false
}

$baseDir = Split-Path (Resolve-Path $MyInvocation.MyCommand.Path)
$toolsDir = "$baseDir\tools"
$nugetExec = "$toolsDir\NuGet\NuGet.exe"
$packageOutPutDir = "$baseDir\artifacts"
$nugetTempPath = "NugetTemp"

task default -depends Build

task Clean { 
	if ( -Not (Test-Path $packageOutPutDir))
	{
		New-Item $packageOutPutDir -ItemType Directory | Out-Null
	} else {
		Remove-Item ($packageOutPutDir + '\*.nupkg')
	}
}

task Pack {

	$v1Projects = @("NServiceBus.ActiveMQ.nuspec", "NServiceBus.RabbitMQ.nuspec", "NServiceBus.SqlServer.nuspec", "NServiceBus.Notifications.nuspec")
	
	$nsbVersion = $ProductVersion + "." + $PatchVersion
			
	if($PreRelease -ne '') {
		$nsbVersion = "{0}.{1}-{2}{3}" -f $ProductVersion, $PatchVersion, $PreRelease, ($BuildNumber).PadLeft(4, '0')
	}
				
	(dir -Path $nugetTempPath -Recurse -Filter '*.nuspec') | foreach { 
			   
			Write-Host Creating NuGet spec file for $_.Name
			
			[xml] $nuspec = Get-Content $_.FullName
			
			if([System.Array]::IndexOf($v1Projects, $_.Name) -eq -1){
				$nugetVersion = $ProductVersion + "." + $PatchVersion
			
				if($PreRelease -ne '') {
					$nuspec.package.metadata.title += ' (' + $PreRelease + ')'
					$nugetVersion = "{0}.{1}-{2}{3}" -f $ProductVersion, $PatchVersion, $PreRelease, ($BuildNumber).PadLeft(4, '0')
				}
			} else {
				$nugetVersion = "1.0.0"
			
				if($PreRelease -ne '') {
					$nuspec.package.metadata.title += ' (' + $PreRelease + ')'
					$nugetVersion = "1.0.0-{0}{1}" -f $PreRelease, ($BuildNumber).PadLeft(4, '0')
				}
			}
	
			$nuspec.package.metadata.version = $nugetVersion
			
			$nuspec | Select-Xml '//dependency[starts-with(@id, "NServiceBus")]' |% {
				$_.Node.version = "[$nsbVersion]"
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