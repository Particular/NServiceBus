namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Transport;

    class OwningSatelliteDefinition : SatelliteDefinition
    {
        public OwningSatelliteDefinition(TransportTransactionMode requiredTransportTransactionMode, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, Func<IBuilder, IDispatchMessages, MessageContext, Task> onMessage) : base(null, null, requiredTransportTransactionMode, null, recoverabilityPolicy, onMessage)
        {
        }
    }
}