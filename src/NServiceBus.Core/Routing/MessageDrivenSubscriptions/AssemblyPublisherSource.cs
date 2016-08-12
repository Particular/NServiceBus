namespace NServiceBus
{
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

        public IEnumerable<PublisherTableEntry> Generate(Conventions conventions)
        {
            return messageAssembly.GetTypes().Where(conventions.IsEventType).Select(t => new PublisherTableEntry(t, address));
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Assembly;
    }
}