using NServiceBus.Host;

namespace Subscriber1
{
    class EndpointConfig : IConfigureThisEndpoint,
        As.aServer,
        ISpecify.ToUse.XmlSerialization        
    {
    }
}
