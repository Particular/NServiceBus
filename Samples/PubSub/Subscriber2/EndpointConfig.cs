using NServiceBus;

namespace Subscriber2
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                //this overrides the NServiceBus default convention of IEvent
                .DefiningEventsAs(t=> t.Namespace != null && t.Namespace.StartsWith("MyMessages"))
                .CastleWindsorBuilder() // just to show we can mix and match containers
                .XmlSerializer()
                .UnicastBus()
                    .DoNotAutoSubscribe(); //managed by the class Subscriber2Endpoint
        }
    }
}
