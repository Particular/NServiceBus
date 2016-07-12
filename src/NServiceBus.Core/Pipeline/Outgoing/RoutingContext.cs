namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;
    using Routing;
    using Transport;

    class RoutingContext : OutgoingContext, IRoutingContext
    {
        public RoutingContext(OutgoingMessage messageToDispatch, RoutingStrategy routingStrategy, IBehaviorContext parentContext)
            : this(messageToDispatch, new[]
            {
                routingStrategy
            }, parentContext)
        {
        }

        public RoutingContext(OutgoingMessage messageToDispatch, IReadOnlyCollection<RoutingStrategy> routingStrategies, IBehaviorContext parentContext)
            : base(messageToDispatch.MessageId, messageToDispatch.Headers, parentContext)
        {
            Message = messageToDispatch;
            RoutingStrategies = routingStrategies;
        }

        public OutgoingMessage Message { get; }

        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; }
    }
}