echo off
md .\binaries\temp
copy .\binaries\log4net.dll .\binaries\temp\log4net.dll
copy .\binaries\NServiceBus.Core.dll .\binaries\temp\NServiceBus.Core.dll
copy .\binaries\NServiceBus.dll .\binaries\temp\NServiceBus.dll
copy .\binaries\NServiceBus.Host.exe .\binaries\temp\NServiceBus.Host.exe
.\binaries\temp\NServiceBus.Host.exe /installInfrastructure
rd /s /q .\binaries\temp


if "%ProgramFiles(x86)%XXX"=="XXX" (
set ProgRoot="%ProgramFiles%"
) else (
set ProgRoot="%ProgramFiles(x86)%"
)
for /f "delims=" %%a in (%ProgRoot%) do set ProgRoot=%%~a

set nugetfound=false
set CommonExtensionPath=%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions
set LocalExtensionPath=%localappdata%\Microsoft\VisualStudio\10.0\Extensions


IF EXIST "%CommonExtensionPath%\Microsoft Corporation\NuGet Package Manager" set nugetfound=true
IF EXIST "%LocalExtensionPath%\Microsoft\NuGet Package Manager" set nugetfound=true
  	
IF %nugetfound%==false ECHO NuGet extension for visual studio is a prerequisite for the NServiceBus modeling tools. Please install the nuget extension manually - http://visualstudiogallery.msdn.microsoft.com/27077b70-9dad-4c64-adcf-c7cf6bc9970c?SRC=Home


set nsbstudioinstalled=false
IF EXIST "%CommonExtensionPath%\NServiceBus\NServiceBus Studio" set nsbstudioinstalled=true
IF EXIST "%LocalExtensionPath%\NServiceBus\NServiceBus Studio" set nsbstudioinstalled=true

IF %nsbstudioinstalled%==false .\tools\NServiceBusStudio.vsix
IF %nsbstudioinstalled%==true ECHO NServiceBus studio already installed - Please visit the visual studio gallery to make sure that you have the latest version

