using NServiceBus.Host;

namespace V2Publisher
{
    class EndpointConfig : IConfigureThisEndpoint,
                            As.aPublisher,
                            ISpecify.ToUseXmlSerialization,
                            ISpecify.ToRun<V2PublisherEndpoint>
    {
    }
}
