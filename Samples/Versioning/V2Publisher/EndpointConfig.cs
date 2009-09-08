using NServiceBus.Host;

namespace V2Publisher
{
    class EndpointConfig : IConfigureThisEndpoint,
                            As.aPublisher,
                            ISpecify.ToUse.XmlSerialization
    {
    }
}
