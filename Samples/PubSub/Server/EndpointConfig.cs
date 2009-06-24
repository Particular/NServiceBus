using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    class EndpointConfig : IConfigureThisEndpoint, ISpecify.ToRun<ServerEndpoint>
    {
        public void Init(Configure configure)
        {
            configure
                .MsmqSubscriptionStorage()
                //.DbSubscriptionStorage()
                //        .Table("Subscriptions")
                //        .SubscriberEndpointColumnName("SubscriberEndpoint")
                //        .MessageTypeColumnName("MessageType")
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
