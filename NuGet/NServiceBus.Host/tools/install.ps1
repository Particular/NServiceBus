param($installPath, $toolsPath, $package, $project)

function Get-ConfigureThisEndpointClass($elem) {

    if ($elem.IsCodeType -and ($elem.Kind -eq [EnvDTE.vsCMElement]::vsCMElementClass)) 
    {            
        foreach ($e in $elem.ImplementedInterfaces) {
            if($e.FullName -eq "NServiceBus.IConfigureThisEndpoint") {
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
        foreach ($codeElem in $item.FileCodeModel.CodeElements) {
            $elem = Get-ConfigureThisEndpointClass($codeElem)
            if($elem -ne $null) {
                return $true
            }
        }
    }

    $false
}

$foundConfigureThisEndpoint = Test-HasConfigureThisEndpoint($project)

Add-BindingRedirect
	
if($foundConfigureThisEndpoint -eq $false) {
	if (Get-Module T4Scaffolding) {
		Scaffold EndpointConfig -Project $project.Name
	}
}
    
#Figure out if this machine has error queue configured in registry
$nserviceBusKeyPath =  "HKLM:SOFTWARE\NServiceBus" 
$regKey = Get-ItemProperty -path $nserviceBusKeyPath -ErrorAction silentlycontinue
$errorQueueAddress  = $regKey.psobject.properties | ?{ $_.Name -eq "ErrorQueue" }
if($errorQueueAddress.value -eq $null -or $errorQueueAddress.value -eq ""){
	Add-NServiceBusMessageForwardingInCaseOfFaultConfig
}

$project.Save()

[xml] $prjXml = Get-Content $project.FullName
foreach($PropertyGroup in $prjXml.project.ChildNodes)
{
	if($PropertyGroup.StartAction -ne $null)
	{
		exit
	}
}

$propertyGroupElement = $prjXml.CreateElement("PropertyGroup", $prjXml.Project.GetAttribute("xmlns"));
$startActionElement = $prjXml.CreateElement("StartAction", $prjXml.Project.GetAttribute("xmlns"));
$propertyGroupElement.AppendChild($startActionElement)
$propertyGroupElement.StartAction = "Program"
$startProgramElement = $prjXml.CreateElement("StartProgram", $prjXml.Project.GetAttribute("xmlns"));
$propertyGroupElement.AppendChild($startProgramElement)
$propertyGroupElement.StartProgram = "`$(ProjectDir)`$(OutputPath)NServiceBus.Host.exe"
$prjXml.project.AppendChild($propertyGroupElement);
$writerSettings = new-object System.Xml.XmlWriterSettings
$writerSettings.OmitXmlDeclaration = $false
$writerSettings.NewLineOnAttributes = $false
$writerSettings.Indent = $true
$projectFilePath = Resolve-Path -Path $project.FullName
$writer = [System.Xml.XmlWriter]::Create($projectFilePath, $writerSettings)
$prjXml.WriteTo($writer)
$writer.Flush()
$writer.Close()
