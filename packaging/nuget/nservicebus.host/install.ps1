param($installPath, $toolsPath, $package, $project)

if(!$toolsPath){
	$project = Get-Project
}

function Get-ConfigureThisEndpointClass($elem) {

    if ($elem.IsCodeType -and ($elem.Kind -eq [EnvDTE.vsCMElement]::vsCMElementClass)) {            
        foreach ($e in $elem.ImplementedInterfaces) {
            if ($e.FullName -eq "NServiceBus.IConfigureThisEndpoint") {
                return $elem
            }
            
            return Get-ConfigureThisEndpointClass($e)
            
        }
    } 
    elseif ($elem.Kind -eq [EnvDTE.vsCMElement]::vsCMElementNamespace) {
        foreach ($e in $elem.Members) {
            $temp = Get-ConfigureThisEndpointClass($e)
            if($temp -ne $null) {
                
                return $temp
            }
        }
    }

    $null
}

function Test-HasConfigureThisEndpoint($project) {
       
    foreach ($item in $project.ProjectItems) {
		if ($projItem.Name -eq "EndpointConfig.cs") {
			return $true
		}
	
		if (HasConfigureThisEndpoint($item)){
			return $true
		}
    }

    $false
}

function HasConfigureThisEndpoint($projItem) {
	foreach ($codeElem in $projItem.FileCodeModel.CodeElements) {
		$elem = Get-ConfigureThisEndpointClass($codeElem)
		if($elem -ne $null) {
			return $true
		}
	}
	
	foreach ($item in $projItem.ProjectItems) {
		return HasConfigureThisEndpoint($item)
	}
}

function Add-StartProgramIfNeeded {
	[xml] $prjXml = Get-Content $project.FullName
	foreach($PropertyGroup in $prjXml.project.ChildNodes)
	{
		if($PropertyGroup.StartAction -ne $null)
		{
			return
		}
	}

    $exeName = "NServiceBus.Host"

    if($package.Id.Contains("Host32"))
    {
        $exeName += "32"
    }

    $exeName += ".exe"

	$propertyGroupElement = $prjXml.CreateElement("PropertyGroup", $prjXml.Project.GetAttribute("xmlns"));
	$startActionElement = $prjXml.CreateElement("StartAction", $prjXml.Project.GetAttribute("xmlns"));
	$propertyGroupElement.AppendChild($startActionElement) | Out-Null
	$propertyGroupElement.StartAction = "Program"
	$startProgramElement = $prjXml.CreateElement("StartProgram", $prjXml.Project.GetAttribute("xmlns"));
	$propertyGroupElement.AppendChild($startProgramElement) | Out-Null
	$propertyGroupElement.StartProgram = "`$(ProjectDir)`$(OutputPath)$exeName"
	$prjXml.project.AppendChild($propertyGroupElement) | Out-Null
	$writerSettings = new-object System.Xml.XmlWriterSettings
	$writerSettings.OmitXmlDeclaration = $false
	$writerSettings.NewLineOnAttributes = $false
	$writerSettings.Indent = $true
	$projectFilePath = Resolve-Path -Path $project.FullName
	$writer = [System.Xml.XmlWriter]::Create($projectFilePath, $writerSettings)
	$prjXml.WriteTo($writer)
	$writer.Flush()
	$writer.Close()
}

function Add-ConfigSettingIfRequired {
	Add-NServiceBusMessageForwardingInCaseOfFaultConfig $project.Name
	Add-NServiceBusUnicastBusConfig $project.Name
    Add-NServiceBusAuditConfig $project.Name
}

function Add-EndpointConfigIfRequired {
	$foundConfigureThisEndpoint = Test-HasConfigureThisEndpoint($project)
	
	if($foundConfigureThisEndpoint -eq $false) {
		$namespace = $project.Properties.Item("DefaultNamespace").Value

		$projectDir = [System.IO.Path]::GetDirectoryName($project.FullName)
		$endpoingConfigPath = [System.IO.Path]::Combine( $projectDir, "EndpointConfig.cs")
		Get-Content  "$installPath\Tools\EndpointConfig.cs" | ForEach-Object { $_ -replace "rootnamespace", $namespace } | Set-Content ($endpoingConfigPath)

		$project.ProjectItems.AddFromFile( $endpoingConfigPath )
	}
}

Add-EndpointConfigIfRequired
    
Add-ConfigSettingIfRequired

$project.Save()

Add-StartProgramIfNeeded
