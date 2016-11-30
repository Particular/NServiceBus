namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Routing;
    using Transport;

    class UnicastRoutingContext : OutgoingContext, IUnicastRoutingContext
    {
        public UnicastRoutingContext(OutgoingMessage messageToDispatch, IReadOnlyCollection<UnicastRoute> destinations, Func<string[], string[]> distributionFunction, IBehaviorContext parentContext)
            : base(messageToDispatch.MessageId, messageToDispatch.Headers, parentContext)
        {
            Message = messageToDispatch;
            Destinations = destinations;
            DistributionFunction = distributionFunction;
        }

        public OutgoingMessage Message { get; }
        public IReadOnlyCollection<UnicastRoute> Destinations { get; }
        public Func<string[], string[]> DistributionFunction { get; }
    }
}