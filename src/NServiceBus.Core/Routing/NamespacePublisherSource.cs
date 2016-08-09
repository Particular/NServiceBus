namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Routing.MessageDrivenSubscriptions;

    class NamespacePublisherSource : IPublisherSource
    {
        Assembly messageAssembly;
        string messageNamespace;
        Conventions conventions;
        PublisherAddress address;

        public NamespacePublisherSource(Assembly messageAssembly, string messageNamespace, Conventions conventions, PublisherAddress address)
        {
            this.messageAssembly = messageAssembly;
            this.conventions = conventions;
            this.address = address;
            this.messageNamespace = messageNamespace;
        }

        public void Generate(Action<PublisherTableEntry> registerPublisherCallback)
        {
            foreach (var type in messageAssembly.GetTypes().Where(t => t.Namespace == messageNamespace).Where(t => conventions.IsMessageType(t)))
            {
                registerPublisherCallback(new PublisherTableEntry(type, address));
            }
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Namespace;
    }
}