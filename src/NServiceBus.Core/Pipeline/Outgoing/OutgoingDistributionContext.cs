namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;
    using Routing;

    class OutgoingDistributionContext : OutgoingContext, IOutgoingDistributionContext
    {
        public OutgoingDistributionContext(string messageId, Dictionary<string, string> headers, OutgoingLogicalMessage message, IReadOnlyCollection<UnicastRoute> destinations, DistributionStrategyScope distributionScope, IBehaviorContext parentContext)
            : base(messageId, headers, parentContext)
        {
            Message = message;
            Destinations = destinations;
            DistributionScope = distributionScope;
            Set(message);
        }

        public OutgoingLogicalMessage Message { get; }
        public IReadOnlyCollection<UnicastRoute> Destinations { get; }
        public DistributionStrategyScope DistributionScope { get; }
    }
}