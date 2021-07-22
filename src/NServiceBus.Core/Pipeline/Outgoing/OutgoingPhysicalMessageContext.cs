namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Routing;

    class OutgoingPhysicalMessageContext : OutgoingContext, IOutgoingPhysicalMessageContext
    {
        public OutgoingPhysicalMessageContext(ReadOnlyMemory<byte> body, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingLogicalMessageContext parentContext)
            : base(parentContext.MessageId, parentContext.Headers, parentContext)
        {
            Body = body;
            RoutingStrategies = routingStrategies;
        }

        public ReadOnlyMemory<byte> Body { get; private set; }

        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; }

        public void UpdateMessage(ReadOnlyMemory<byte> body)
        {
            Body = body;
        }
    }
}