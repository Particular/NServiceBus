using NServiceBus;

namespace Subscriber2
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            NServiceBus.Configure.With()
                .CastleWindsorBuilder() // just to show we can mix and match containers
                .XmlSerializer()
                .UnicastBus()
                    .DoNotAutoSubscribe(); //managed by the class Subscriber2Endpoint
        }
    }
}
