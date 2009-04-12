cd src\ObjectBuilder
msbuild
cd ..\..\src\core
msbuild
cd ..\..\src\utils
msbuild
cd ..\..\src\messageInterfaces
msbuild
cd ..\..\src\impl\messageInterfaces
msbuild
cd ..\..\..\src\config
msbuild
cd ..\..\src\unicast
msbuild 
cd ..\..\src\impl\unicast\NServiceBus.Unicast.Msmq
msbuild
cd ..\..\..\..\src\impl\unicast\NServiceBus.Unicast.Subscriptions.Msmq
msbuild
cd ..\..\..\..\src\impl\unicast\NServiceBus.Unicast.Subscriptions.DB
msbuild
cd ..\..\..\..\src\impl\SagaPersisters\NHibernateSagaPersister
msbuild
cd ..\..\..\..\src\impl\Serializers
msbuild
cd ..\..\..\src\impl\ObjectBuilder
msbuild
cd ..\..\..\src\multicast\NServiceBus.Multicast
msbuild
cd ..\..\..\src\grid
msbuild
cd ..\..