[T4Scaffolding.Scaffolder(Description = "Creates a EndpointConfig code file")][CmdletBinding()]
param(        
    [string]$Project,
	[string[]]$TemplateFolders,
	[switch]$Force = $false
)

$outputPath = "EndpointConfig"  # The filename extension will be added based on the template's <#@ Output Extension="..." #> directive
$namespace = (Get-Project $Project).Properties.Item("DefaultNamespace").Value

Add-ProjectItemViaTemplate $outputPath -Template EndpointConfigTemplate `
	-Model @{ Namespace = $namespace; } `
	-SuccessMessage "Added EndpointConfig output at {0}" `
	-TemplateFolders $TemplateFolders -Project $Project -CodeLanguage $CodeLanguage -Force:$Force