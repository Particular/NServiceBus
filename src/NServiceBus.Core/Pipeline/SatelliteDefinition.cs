namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class SatelliteDefinition
    {
        public SatelliteDefinition(string name, string receiveAddress, TransportTransactionMode requiredTransportTransactionMode, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, Func<IServiceProvider, MessageContext, CancellationToken, Task> onMessage)
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

        public Func<IServiceProvider, MessageContext, CancellationToken, Task> OnMessage { get; }
    }
}