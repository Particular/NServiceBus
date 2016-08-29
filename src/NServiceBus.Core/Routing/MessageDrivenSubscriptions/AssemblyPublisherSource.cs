namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Routing.MessageDrivenSubscriptions;

    class AssemblyPublisherSource : IPublisherSource
    {
        Assembly messageAssembly;
        PublisherAddress address;

        public AssemblyPublisherSource(Assembly messageAssembly, PublisherAddress address)
        {
            this.messageAssembly = messageAssembly;
            this.address = address;
        }

        public IEnumerable<PublisherTableEntry> GenerateWithBestPracticeEnforcement(Conventions conventions)
        {
            var entries = messageAssembly.GetTypes()
                .Where(conventions.IsEventType)
                .Select(t => new PublisherTableEntry(t, address))
                .ToArray();

            if (!entries.Any())
            {
                throw new Exception($"Cannot configure publisher for assembly {messageAssembly.GetName().Name} because it contains no types considered as events. Event types have to either implement NServiceBus.IEvent interface or match a defined event convention.");
            }

            return entries;
        }

        public IEnumerable<PublisherTableEntry> GenerateWithoutBestPracticeEnforcement(Conventions conventions)
        {
            var entries = messageAssembly.GetTypes()
                .Where(type => conventions.IsMessageType(type) && !conventions.IsCommandType(type))
                .Select(t => new PublisherTableEntry(t, address))
                .ToArray();

            if (!entries.Any())
            {
                throw new Exception($"Cannot configure publisher for assembly {messageAssembly.GetName().Name} because it contains no types considered as messages. Message types have to either implement NServiceBus.IMessage interface or match a defined convention.");
            }

            return entries;
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Assembly;
    }
}