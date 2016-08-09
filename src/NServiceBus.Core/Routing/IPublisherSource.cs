namespace NServiceBus
{
    using System;
    using Routing.MessageDrivenSubscriptions;

    interface IPublisherSource
    {
        void Generate(Action<PublisherTableEntry> registerPublisherCallback);
        RouteSourcePriority Priority { get; }
    }
}