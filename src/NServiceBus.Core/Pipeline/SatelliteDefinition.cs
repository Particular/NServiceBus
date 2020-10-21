using System.Collections.Generic;

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
            var satellitePushSettings = new PushSettings(ReceiveAddress, errorQueue, purgeOnStartup, RequiredTransportTransactionMode);

            return new ReceiveSettings
            {
                Id = Name,
                ReceiveAddress = ReceiveAddress,
                settings = satellitePushSettings,
                UsePublishSubscribe = false
            };
            //satelliteReceiver = await transportInfrastructure.CreateReceiver().ConfigureAwait(false);
        }

        public void Start(IPushMessages satelliteReceiver, IServiceProvider builder, RecoverabilityExecutorFactory recoverabilityExecutorFactory)
        {
            this.satelliteReceiver = satelliteReceiver;

            var satellitePipeline = new SatellitePipelineExecutor(builder, this);
            var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(RecoverabilityPolicy, ReceiveAddress);
            satelliteReceiver.Start(RuntimeSettings, satellitePipeline.Invoke, satelliteRecoverabilityExecutor.Invoke);
        }

        public Task Stop()
        {
            return satelliteReceiver.Stop();
        }

        private IPushMessages satelliteReceiver;
    }
}