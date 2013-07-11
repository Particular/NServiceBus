using NServiceBus;

namespace OrderService
{
    using NServiceBus.Features;

    public class EndpointConfiguration : IConfigureThisEndpoint, AsA_Worker, UsingTransport<AzureStorageQueue>
    {
        public EndpointConfiguration()
        {
            Feature.Disable<Gateway>();
            Feature.Disable<SecondLevelRetries>();
            Feature.Disable<TimeoutManager>();
        }
    }
}