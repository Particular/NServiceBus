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
    }
}