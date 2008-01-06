cd src\ObjectBuilder
msbuild
cd ..\..\src\core
msbuild 
cd ..\..\src\utils
msbuild
cd ..\..\src\unicast
msbuild 
cd ..\..\src\impl\unicast\NServiceBus.Unicast.Msmq
msbuild
cd ..\..\..\..\src\impl\unicast\NServiceBus.Unicast.Subscriptions.Msmq
msbuild
cd ..\..\..\..\src\impl\unicast\NServiceBus.Unicast.Transport.WCF
msbuild
cd ..\..\..\..\src\impl\SagaPersisters\DbBlobSagaPersister
msbuild
cd ..\..\..\..\src\impl\ObjectBuilder.SpringFramework
msbuild
cd ..\..\..\src\multicast\NServiceBus.Multicast
msbuild
cd ..\..\..\src\impl\multicast\NServiceBus.Multicast.Transport.Udp
msbuild
cd ..\..\..\..\src\grid
msbuild
cd ..\..\..\src\distributor\NServiceBus.Unicast.Distributor
msbuild
cd ..\..\..\src\distributor\MsmqWorkerAvailabilityManager
msbuild
cd ..\..\..\src\distributor\NServiceBus.Unicast.Distributor.Runner
msbuild
cd ..\..\src\tools\management\Grid
msbuild
cd ..\..\..\..\test\Messages
msbuild
cd ..\..\test\Client
msbuild
cd ..\..\test\Server
msbuild
cd ..\..\test\Worker
msbuild
cd ..\..\test\RequestResponse
msbuild
cd ..\..\Samples\AsyncPages
msbuild
cd ..\..\Samples\WebServiceBridge
msbuild
cd ..\..\
