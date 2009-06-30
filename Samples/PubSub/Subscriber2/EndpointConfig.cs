using NServiceBus.Host;

namespace Subscriber2
{
    class EndpointConfig : IConfigureThisEndpoint, 
        As.aServer,
        ISpecify.ToUseXmlSerialization
    {
    }
}
