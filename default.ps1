task default -depends CreatePackage

task CreatePackage -depends  BuildOnNet35, BuildOnNet40  {
	import-module ./NuGet\packit.psm1
	Write-Output "Loding the moduele for packing.............."
	$packit.push_to_nuget = $false 
	
	#region Packing NserviceBusWithSymb
	$packit.package_description = "The most popular open-source service bus for .net"
	invoke-packit "NserviceBusWithSymb" "" @{log4net="1.2.10"} "NServiceBus.dll", "NServiceBus.Core.dll","NServiceBus.pdb","NServiceBus.Core.pdb" @{".\src\core\NServiceBus\*.cs"="src\core\NServiceBus";".\src\core\NServiceBus\Properties\*cs"="src\core\NServiceBus\Properties"} $true
	#endregion
	
	#region Packing NserviceBus
	$packit.package_description = "The most popular open-source service bus for .net"
	invoke-packit "NServiceBus" "" @{log4net="1.2.10"} "NServiceBus.dll", "NServiceBus.Core.dll","NServiceBus.pdb","NServiceBus.Core.pdb" @{".\src\core\NServiceBus\*.cs"="src\core\NServiceBus";".\src\core\NServiceBus\Properties\*cs"="src\core\NServiceBus\Properties"}
	#endregion
	
	#region Packing NServiceBus.Host
	$packit.package_description = "The hosting template for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit "NServiceBus.Host" "" @{NServiceBus="<version>"} "NServiceBus.Host.exe" 
	#endregion
	
	#region Packing NServiceBus.Testing
	$packit.package_description = "The testing for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit "NServiceBus.Testing" "" @{NServiceBus="<version>"} "NServiceBus.Testing.dll"
	#endregion
	
	#region Packing NServiceBus.Tools
	$packit.package_description = "The tools for configure the nservicebus, The most popular open-source service bus for .net"
	invoke-packit "NServiceBus.Tools" "" @{} "" @{".\tools\msmqutils\*.*"="tools\msmqutils";".\tools\RunMeFirst.bat"="tools";".\tools\install.ps1"="tools"}
	#endregion
	
	#region Packing NServiceBus.Autofac2
	$packit.package_description = "The Autofac Container for the nservicebus, The most popular open-source service bus for .net"
	invoke-packit "NServiceBus.Autofac2" "" @{Autofac="2.3.2.632"} "containers\autofac\NServiceBus.ObjectBuilder.Autofac.dll"
	#endregion
	
	remove-module packit
 }
 
task BuildOnNet35 {
 	.\tools\nant\nant.exe -D:targetframework=net-3.5
	XCopy  binaries\* build\lib\net35\ /S /Y
 }
 
task BuildOnNet40 {
 	.\tools\nant\nant.exe -D:targetframework=net-4.0
	XCopy  binaries\* build\lib\net40\ /S /Y
 }