using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    class EndpointConfig : IConfigureThisEndpoint, 
        As.aServer, 
        ISpecify.ToUseXmlSerialization,
        ISpecify.ToRun<ServerEndpoint>,
        IWantCustomInitialization
    {
        public void Init(Configure configure)
        {
            configure
                .MsmqSubscriptionStorage();
                //.DbSubscriptionStorage()
                //        .Table("Subscriptions")
                //        .SubscriberEndpointColumnName("SubscriberEndpoint")
                //        .MessageTypeColumnName("MessageType")
        }
    }
}
