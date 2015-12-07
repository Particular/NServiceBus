namespace NServiceBus.TransportDispatch
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    class RoutingContextImpl : OutgoingContextImpl, RoutingContext
    {
        public RoutingContextImpl(OutgoingMessage messageToDispatch, RoutingStrategy addressLabel, BehaviorContext context)
            : this(messageToDispatch, new[] { addressLabel }, context)
        {
        }

        public RoutingContextImpl(OutgoingMessage messageToDispatch, IReadOnlyCollection<RoutingStrategy> routingStrategies, BehaviorContext context)
            : base(messageToDispatch.MessageId, messageToDispatch.Headers, context)
        {
            Message = messageToDispatch;
            RoutingStrategies = routingStrategies;
        }

        public OutgoingMessage Message { get; }

        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; }
    }
}