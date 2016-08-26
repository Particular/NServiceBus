namespace NServiceBus
{
    using System.Collections.Generic;
    using Routing.MessageDrivenSubscriptions;

    interface IPublisherSource
    {
        IEnumerable<PublisherTableEntry> GenerateWithBestPracticeEnforcement(Conventions conventions);
        IEnumerable<PublisherTableEntry> GenerateWithoutBestPracticeEnforcement(Conventions conventions);
        RouteSourcePriority Priority { get; }
    }
}