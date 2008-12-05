del build\*.* /Q
del external-bin\NServiceBus.dll
del external-bin\NServiceBus.pdb
del external-bin\NServiceBus.xml
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
cd ..\..\..\src\config\NServiceBus.Config
msbuild
cd ..\..\..\src\unicast
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
cd ..\..\..\src\impl\ObjectBuilder.SpringFramework
msbuild
cd ..\..\..\src\multicast\NServiceBus.Multicast
msbuild
cd ..\..\..\src\grid
msbuild
cd ..\..
xcopy build build\merge /Q /Y
xcopy external-bin build\merge /Q /Y
external-bin\ilmerge /log:build\output.txt /target:library /xmldocs /out:NServiceBus.dll build\merge\NServiceBus.dll build\merge\NServiceBus.Config.dll build\merge\Interop.MSMQ.dll build\merge\NServiceBus.Grid.MessageHandlers.dll build\merge\NServiceBus.Grid.Messages.dll build\merge\NServiceBus.MessageInterfaces.dll build\merge\NServiceBus.MessageInterfaces.MessageMapper.Reflection.dll build\merge\NServiceBus.Multicast.dll build\merge\NServiceBus.Multicast.Transport.dll build\merge\NServiceBus.Saga.dll build\merge\NServiceBus.SagaPersisters.NHibernate.dll build\merge\NServiceBus.SagaPersisters.NHibernate.Config.dll build\merge\NServiceBus.Serialization.dll build\merge\NServiceBus.Serializers.Binary.dll build\merge\NServiceBus.Serializers.Configure.dll build\merge\NServiceBus.Serializers.XML.dll build\merge\NServiceBus.Unicast.Config.dll build\merge\NServiceBus.Unicast.dll build\merge\NServiceBus.Unicast.Subscriptions.DB.dll build\merge\NServiceBus.Unicast.Subscriptions.DB.Config.dll build\merge\NServiceBus.Unicast.Subscriptions.dll build\merge\NServiceBus.Unicast.Subscriptions.Msmq.dll build\merge\NServiceBus.Unicast.Subscriptions.Msmq.Config.dll build\merge\NServiceBus.Unicast.Transport.dll build\merge\NServiceBus.Unicast.Transport.Msmq.dll build\merge\NServiceBus.Unicast.Transport.Msmq.Config.dll build\merge\NServiceBus.Utils.dll
del build\merge\*.* /Q
move NServiceBus.dll external-bin
move NServiceBus.pdb external-bin
move NServiceBus.xml external-bin
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
cd ..\..\Samples\AsyncPages
msbuild
cd ..\..\Samples\WebServiceBridge
msbuild
cd ..\..\Samples\FullDuplex
msbuild
cd ..\..\Samples\PubSub
msbuild
cd ..\..\Samples\Manufacturing
msbuild
cd ..\..\
