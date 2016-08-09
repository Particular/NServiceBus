namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Routing.MessageDrivenSubscriptions;

    class AssemblyPublisherSource : IPublisherSource
    {
        Assembly messageAssembly;
        Conventions conventions;
        PublisherAddress address;

        public AssemblyPublisherSource(Assembly messageAssembly, Conventions conventions, PublisherAddress address)
        {
            this.messageAssembly = messageAssembly;
            this.conventions = conventions;
            this.address = address;
        }

        public void Generate(Action<PublisherTableEntry> registerPublisherCallback)
        {
            foreach (var type in messageAssembly.GetTypes().Where(t => conventions.IsEventType(t)))
            {
                registerPublisherCallback(new PublisherTableEntry(type, address));
            }
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Assembly;
    }
}