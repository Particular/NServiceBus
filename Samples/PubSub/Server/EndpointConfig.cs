using NServiceBus.Host;

namespace Server
{
    class EndpointConfig : IConfigureThisEndpoint, 
        As.aServer, As.aPublisher,
        ISpecify.ToUseXmlSerialization,
        ISpecify.ToRun<ServerEndpoint>
    {
    }
}
