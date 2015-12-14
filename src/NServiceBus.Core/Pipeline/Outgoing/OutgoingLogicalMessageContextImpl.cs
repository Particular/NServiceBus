namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;

    class OutgoingLogicalMessageContextImpl : OutgoingContextImpl, OutgoingLogicalMessageContext
    {
        public OutgoingLogicalMessageContextImpl(string messageId, Dictionary<string, string> headers, OutgoingLogicalMessage message, IReadOnlyCollection<RoutingStrategy> routingStrategies, BehaviorContext parentContext)
            : base(messageId, headers, parentContext)
        {
            Message = message;
            RoutingStrategies = routingStrategies;
            Set(message);
        }

        public OutgoingLogicalMessage Message { get; private set; }

        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; private set; }

        public void UpdateMessageInstance(object newInstance)
        {
            Guard.AgainstNull(nameof(newInstance), newInstance);

            Message = new OutgoingLogicalMessage(newInstance);
        }
    }
}