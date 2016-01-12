namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;

    class OutgoingPhysicalMessageContext : OutgoingContext, IOutgoingPhysicalMessageContext
    {
        public OutgoingPhysicalMessageContext(byte[] body, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingLogicalMessageContext parentContext)
            : base(parentContext.MessageId, parentContext.Headers, parentContext)
        {
            Body = body;
            RoutingStrategies = routingStrategies;
        }

        public byte[] Body { get; set; }

        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; } 
    }
}