#-- Public Module Variables -- 
$script:packit = @{}
$script:packit.push_to_nuget = $false      # Set the variable to true to push the package to NuGet galary.
$script:packit.default_package = "NServiceBus"
$script:packit.package_owners = "Udi Dahan, Andreas Ohlund, Matt Burton, Jonathan Oliver et al"
$script:packit.package_authors = "Udi Dahan, Andreas Ohlund, Matt Burton, Jonathan Oliver et al"
$script:packit.package_description = "The hosting template for the nservicebusThe most popular open-source service bus for .net"
$script:packit.package_language = "en-US"
$script:packit.package_licenseUrl = "http://nservicebus.com/license.aspx"
$script:packit.package_projectUrl = "http://nservicebus.com/"
$script:packit.package_requireLicenseAcceptance = $true;
$script:packit.package_tags = "nservicebus servicebus msmq cqrs publish subscribe"
$script:packit.package_version = "2.5"
$script:packit.build_Location = "..\..\..\Build"
$script:packit.versionAssembly = $script:packit.build_Location + "\nservicebus\NServiceBus.dll"

Export-ModuleMember -Variable "packit"

function GetVersionFromVersionAssembly()
{
	$versionAssemblyLocation = Resolve-Path -Path $script:packit.versionAssembly
	$Myasm = [System.Reflection.Assembly]::Loadfile($versionAssemblyLocation)
	return $Myasm.Version;
}

function Invoke-Packit
{

[CmdletBinding(
    SupportsShouldProcess=$False,
    SupportsTransactions=$False,
    ConfirmImpact="None",
    DefaultParameterSetName="")]
	
	param(
    		 [Parameter(Position=0,Mandatory=0)]
    		 [string]$packageName = $script:packit.default_package,
			 [Parameter(Position=1,Mandatory=0)]
    		 [string]$packageVersion = "",
			 [Parameter(Position=2,Mandatory=0)]
    		 [System.Collections.Hashtable]$dependencies,
			 [Parameter(Position=3, Mandatory=0)]
			 [System.Collections.Hashtable]$assemplyNames = @{}
  		)
		
	begin
	{
	
	}
	process
	{
		$version = $packageVersion;
		if($version -eq "")
		{
			try
			{
				 $version = GetVersionFromVersionAssembly
			}
			catch
			{
			  "Unable to Find the Version from assembly due to the Error:- `n $_"
		      $version = $script:packit.package_version
			}
			
			$packageDir = "..\" + $packageName
			del $packageDir
			mkdir $packageDir
			$packagePath = $packageDir + "\" + $packageName
			NuGet spec $packagePath
			<#
			 Logic to Edit the .nuspec according to the parameter
			 
			#>
			
			<#
			 Logic Copy the assemblies to lib
			 Logic to Copy the Content
			 Logic to copy the tools
			 
			#>
			
		}
	}
	end
	{
	
	}
	
}

Export-ModuleMember -Function "Invoke-Packit"

