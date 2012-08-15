param($installPath, $toolsPath, $package, $project)
	
    $project.Save()
    
	$directoryName  = [system.io.Path]::GetDirectoryName($project.FullName)	
	$appConfigFile = $directoryName + "\App.config"
	if((Test-Path -Path $appConfigFile) -eq $true){
		[xml] $appConfig = Get-Content $appConfigFile
		$selectedNodes = Select-Xml -XPath "/configuration/nr:trueessageForwardingInCaseOfFaultConfig" -Xml $appConfig
		if($selectedNodes -ne $null){
			$selectedNodes.Count
			if($selectedNodes.Count -gt 1){
				$selectedNode = Select-Xml -XPath "/configuration/nr:trueessageForwardingInCaseOfFaultConfig[@ErrorQueue='error' ]" -Xml $appConfig
				$appConfig | select-xml -xpath "/configuration" | % {$_.node.removechild($selectedNode.node)}
				$writerSettings = new-object System.Xml.XmlWriterSettings
				$writerSettings.OmitXmlDeclaration = $false
				$writerSettings.NewLineOnAttributes = $false
				$writerSettings.Indent = $true			
				$writer = [System.Xml.XmlWriter]::Create($appConfigFile, $writerSettings)
				$appConfig.WriteTo($writer)
				$writer.Flush()
				$writer.Close()
			}
		}
	}
	
	[xml] $prjXml = Get-Content $project.FullName
	$proceed = $true
	foreach($PropertyGroup in $prjXml.project.ChildNodes)
	{
	  if($PropertyGroup.StartAction -ne $null)
	  {
		$proceed = $false
	  }
	}

	if ($proceed -eq $true){
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
	} 

