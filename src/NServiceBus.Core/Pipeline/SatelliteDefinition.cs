namespace NServiceBus
{
    using System;
    using Transport;

    class SatelliteDefinition
    {
        public SatelliteDefinition(string name, QueueAddress receiveAddress, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, OnSatelliteMessage onMessage)
        {
            Name = name;
            ReceiveAddress = receiveAddress;
            RuntimeSettings = runtimeSettings;
            RecoverabilityPolicy = recoverabilityPolicy;
            OnMessage = onMessage;
        }

        public string Name { get; }

        public QueueAddress ReceiveAddress { get; }

        public PushRuntimeSettings RuntimeSettings { get; }

        public Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> RecoverabilityPolicy { get; }

        public OnSatelliteMessage OnMessage { get; }
    }
}