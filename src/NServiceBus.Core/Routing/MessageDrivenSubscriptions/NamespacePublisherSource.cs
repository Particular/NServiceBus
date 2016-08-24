namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Routing.MessageDrivenSubscriptions;

    class NamespacePublisherSource : IPublisherSource
    {
        Assembly messageAssembly;
        string messageNamespace;
        PublisherAddress address;

        public NamespacePublisherSource(Assembly messageAssembly, string messageNamespace, PublisherAddress address)
        {
            this.messageAssembly = messageAssembly;
            this.address = address;
            this.messageNamespace = messageNamespace;
        }

        public IEnumerable<PublisherTableEntry> Generate(Conventions conventions)
        {
            var entries = messageAssembly.GetTypes()
                .Where(t => conventions.IsEventType(t) && t.Namespace == messageNamespace)
                .Select(t => new PublisherTableEntry(t, address))
                .ToArray();

            if (!entries.Any())
            {
                throw new Exception($"Cannot configure publisher for namespace {messageNamespace} because it contains no types considered as events. Event types have to either implement NServiceBus.IEvent interface or follow a defined event convention.");
            }

            return entries;
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Namespace;
    }
}