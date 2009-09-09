using NServiceBus.Host;

namespace Server
{
    internal class EndpointConfig : IConfigureThisEndpoint,
                                    As.aPublisher,
                                    ISpecify.ToUse.XmlSerialization
    {
    }
}