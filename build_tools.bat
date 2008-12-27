cd src\testing
msbuild
cd ..\..\
xcopy external-bin\Rhino.Mocks.* build\merge /Q /Y
external-bin\ilmerge /target:library /xmldocs /out:NServiceBus.Testing.dll build\merge\NServiceBus.Testing.dll build\merge\Rhino.Mocks.dll
echo NServiceBus.Testing.dll merged
move NServiceBus.Testing.dll build\output
move NServiceBus.Testing.pdb build\output
move NServiceBus.Testing.xml build\output
del build\merge\*.* /Q
cd src\distributor\NServiceBus.Unicast.Distributor
msbuild
cd ..\..\..\src\distributor\MsmqWorkerAvailabilityManager
msbuild
cd ..\..\..\src\distributor\NServiceBus.Unicast.Distributor.Runner
msbuild
cd ..\..\..\src\tools\management\Grid
msbuild
cd ..\..\..\..\src\tools\management\Errors\ReturnToSourceQueue
msbuild
cd ..\..\..\..\..\src\timeout
msbuild
cd ..\..