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

        public IEnumerable<PublisherTableEntry> GenerateWithBestPracticeEnforcement(Conventions conventions)
        {
            if (!conventions.IsMessageType(messageType))
            {
                throw new Exception($"Cannot configure publisher for type '{messageType.FullName}' because it is not considered a message. Message types have to either implement NServiceBus.IMessage interface or match a defined message convention.");
            }
            if (!conventions.IsEventType(messageType))
            {
                throw new Exception($"Cannot configure publisher for type '{messageType.FullName}' because it is not considered an event. Event types have to either implement NServiceBus.IEvent interface or match a defined event convention.");
            }
            yield return new PublisherTableEntry(messageType, address);
        }

        public IEnumerable<PublisherTableEntry> GenerateWithoutBestPracticeEnforcement(Conventions conventions)
        {
            if (!conventions.IsMessageType(messageType))
            {
                throw new Exception($"Cannot configure publisher for type '{messageType.FullName}' because it is not considered a message. Message types have to either implement NServiceBus.IMessage interface or match a defined message convention.");
            }
            if (conventions.IsCommandType(messageType))
            {
                throw new Exception($"Cannot configure publisher for type '{messageType.FullName}' because it is a command.");
            }
            yield return new PublisherTableEntry(messageType, address);
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Type;
    }
}