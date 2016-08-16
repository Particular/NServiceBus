namespace NServiceBus
{
    using System.Collections.Generic;
    using Routing.MessageDrivenSubscriptions;

    interface IPublisherSource
    {
        IEnumerable<PublisherTableEntry> Generate(Conventions conventions);
        RouteSourcePriority Priority { get; }
    }
}