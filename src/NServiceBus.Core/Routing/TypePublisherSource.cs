namespace NServiceBus
{
    using System;
    using Routing.MessageDrivenSubscriptions;

    class TypePublisherSource : IPublisherSource
    {
        Type messageType;
        PublisherAddress address;

        public TypePublisherSource(Type messageType, PublisherAddress address)
        {
            this.messageType = messageType;
            this.address = address;
        }

        public void Generate(Action<PublisherTableEntry> registerPublisherCallback)
        {
            registerPublisherCallback(new PublisherTableEntry(messageType, address));
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Type;
    }
}