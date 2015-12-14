namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;

    class OutgoingPhysicalMessageContextImpl : OutgoingContextImpl, OutgoingPhysicalMessageContext
    {
        public OutgoingPhysicalMessageContextImpl(byte[] body, IReadOnlyCollection<RoutingStrategy> routingStrategies, OutgoingLogicalMessageContext parentContext)
            : base(parentContext.MessageId, parentContext.Headers, parentContext)
        {
            Body = body;
            RoutingStrategies = routingStrategies;
        }

        public byte[] Body { get; set; }

        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; } 
    }
}