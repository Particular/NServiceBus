using System;
using NServiceBus.Host;
using NServiceBus;

namespace Subscriber2
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization
    {
        public void Init()
        {
            NServiceBus.Configure.With()
                .CastleWindsorBuilder() // just to show we can mix and match containers
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                .UnicastBus()
                    .DoNotAutoSubscribe() //managed by the class Subscriber2Endpoint
                    .LoadMessageHandlers();
        }
    }
}
