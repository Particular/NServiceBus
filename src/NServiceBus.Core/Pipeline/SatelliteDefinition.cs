using System.Collections.Generic;
using System.Threading;
using NServiceBus.Unicast.Messages;

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class SatelliteDefinition
    {
        public SatelliteDefinition(string name, string receiveAddress, TransportTransactionMode requiredTransportTransactionMode, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, Func<IServiceProvider, MessageContext, Task> onMessage)
        {
            Name = name;
            ReceiveAddress = receiveAddress;
            RequiredTransportTransactionMode = requiredTransportTransactionMode;
            RuntimeSettings = runtimeSettings;
            RecoverabilityPolicy = recoverabilityPolicy;
            OnMessage = onMessage;
        }

        public string Name { get; }

        public string ReceiveAddress { get; }

        public TransportTransactionMode RequiredTransportTransactionMode { get; }

        public PushRuntimeSettings RuntimeSettings { get; }

        public Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> RecoverabilityPolicy { get; }

        public Func<IServiceProvider, MessageContext, Task> OnMessage { get; }

        public ReceiveSettings Setup(string errorQueue, bool purgeOnStartup)
        {
            return new ReceiveSettings(Name, ReceiveAddress, false, purgeOnStartup, errorQueue,
                RequiredTransportTransactionMode, new MessageMetadata[0]);
            //satelliteReceiver = await transportInfrastructure.CreateReceiver().ConfigureAwait(false);
        }

        public Task Start(IPushMessages satelliteReceiver, IServiceProvider builder, RecoverabilityExecutorFactory recoverabilityExecutorFactory)
        {
            this.satelliteReceiver = satelliteReceiver;

            var satellitePipeline = new SatellitePipelineExecutor(builder, this);
            var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(RecoverabilityPolicy, ReceiveAddress);
            return satelliteReceiver.Start(RuntimeSettings, satellitePipeline.Invoke, satelliteRecoverabilityExecutor.Invoke, CancellationToken.None);
        }

        public Task Stop()
        {
            return satelliteReceiver.Stop(CancellationToken.None);
        }

        private IPushMessages satelliteReceiver;
    }
}