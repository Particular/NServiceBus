using NServiceBus;
using NServiceBus.Host;

namespace V2Subscriber
{
    public class V2SubscriberEndpoint : IConfigureThisEndpoint
    {
        public void Init(Configure configure)
        {
            configure
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .LoadMessageHandlers();
        }
    }
}