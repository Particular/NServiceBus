namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;
    using Routing;

    class OutgoingLogicalMessageContext : OutgoingContext, IOutgoingLogicalMessageContext
    {
        public OutgoingLogicalMessageContext(string messageId, Dictionary<string, string> headers, OutgoingLogicalMessage message, IReadOnlyCollection<RoutingStrategy> routingStrategies, IBehaviorContext parentContext)
            : base(messageId, headers, parentContext)
        {
            Message = message;
            RoutingStrategies = routingStrategies;
            Set(message);
        }

        public OutgoingLogicalMessage Message { get; private set; }

        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; }

        public void UpdateMessage(object newInstance)
        {
            Guard.AgainstNull(nameof(newInstance), newInstance);

            Message = new OutgoingLogicalMessage(newInstance.GetType(), newInstance);
        }
    }
}