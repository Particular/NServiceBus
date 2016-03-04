namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;
    using Routing;

    class OutgoingPhysicalMessageContext : OutgoingContext, IOutgoingPhysicalMessageContext
    {
        public OutgoingPhysicalMessageContext(byte[] body, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingLogicalMessageContext parentContext)
            : base(parentContext.MessageId, parentContext.Headers, parentContext)
        {
            Body = body;
            RoutingStrategies = routingStrategies;
        }

        public byte[] Body { get; private set; }

        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; }

        public void UpdateMessage(byte[] body)
        {
            Body = body;
        }
    }
}