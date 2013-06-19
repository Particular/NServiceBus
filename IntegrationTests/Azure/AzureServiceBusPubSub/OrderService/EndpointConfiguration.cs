using NServiceBus;

namespace OrderService
{
    using NServiceBus.Features;

    public class EndpointConfiguration : IConfigureThisEndpoint, AsA_Worker, UsingTransport<AzureServiceBus>
    {
        public EndpointConfiguration()
        {
            Feature.Disable<SecondLevelRetries>();
            Feature.Disable<TimeoutManager>();
        }
    }
}