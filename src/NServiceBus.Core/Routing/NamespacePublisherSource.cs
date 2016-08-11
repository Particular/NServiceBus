namespace NServiceBus
{
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
            return messageAssembly.GetTypes()
                .Where(t => conventions.IsEventType(t) && t.Namespace == messageNamespace)
                .Select(t => new PublisherTableEntry(t, address));
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Namespace;
    }
}