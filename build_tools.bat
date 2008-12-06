cd src\testing
msbuild
cd ..\..\src\distributor\NServiceBus.Unicast.Distributor
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