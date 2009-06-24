using NServiceBus;
using NServiceBus.Host;

namespace V2Publisher
{
    class EndpointConfig : IConfigureThisEndpoint, ISpecify.ToRun<V2PublisherEndpoint>
    {
        public void Init(Configure configure)
        {
            configure
                .SpringBuilder()
                .MsmqSubscriptionStorage()
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false);
        }
    }
}
