using NServiceBus;

namespace Subscriber2
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder() //TODO - until we fix the bug in the castle builder .CastleWindsorBuilder() // just to show we can mix and match containers
                .XmlSerializer()
                .UnicastBus()
                    .DoNotAutoSubscribe(); //managed by the class Subscriber2Endpoint
        }
    }
}
