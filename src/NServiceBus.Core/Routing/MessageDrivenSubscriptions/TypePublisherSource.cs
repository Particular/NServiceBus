namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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

        public IEnumerable<PublisherTableEntry> Generate(Conventions conventions)
        {
            yield return new PublisherTableEntry(messageType, address);
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Type;
    }
}