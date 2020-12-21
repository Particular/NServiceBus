using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using NServiceBus.Transports;
using NServiceBus.Unicast.Messages;

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class SatelliteDefinition
    {
        IMessageReceiver satelliteReceiver;

        public SatelliteDefinition(string name, string receiveAddress, TransportTransactionMode requiredTransportTransactionMode, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, Func<IServiceProvider, MessageContext, Task> onMessage)
        {
            Name = name;
            ReceiveAddress = receiveAddress;
            RequiredTransportTransactionMode = requiredTransportTransactionMode;
            RuntimeSettings = runtimeSettings;
            RecoverabilityPolicy = recoverabilityPolicy;
            OnMessage = onMessage;
        }

        public async Task Start(IMessageReceiver satelliteReceiver, IServiceProvider builder, RecoverabilityExecutorFactory recoverabilityExecutorFactory)
        {
            this.satelliteReceiver = satelliteReceiver;

            var satellitePipeline = new SatellitePipelineExecutor(builder, this);
            var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(RecoverabilityPolicy, ReceiveAddress);
            
            await satelliteReceiver.Initialize(RuntimeSettings, satellitePipeline.Invoke, satelliteRecoverabilityExecutor.Invoke, new ReadOnlyCollection<MessageMetadata>(new List<MessageMetadata>()), CancellationToken.None).ConfigureAwait(false);

            await satelliteReceiver.StartReceive(CancellationToken.None).ConfigureAwait(false);
        }

        public string Name { get; }

        public string ReceiveAddress { get; }

        public TransportTransactionMode RequiredTransportTransactionMode { get; }

        public PushRuntimeSettings RuntimeSettings { get; }

        public Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> RecoverabilityPolicy { get; }

        public Func<IServiceProvider, MessageContext, Task> OnMessage { get; }
    }
}