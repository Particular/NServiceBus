namespace Subscriber2
{
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public static string BasePath = "..\\..\\..\\storage";
        
        public void Init()
        {
            var config = Configure.With()
                //this overrides the NServiceBus default convention of IEvent
                .CastleWindsorBuilder() // just to show we can mix and match containers
                .FileShareDataBus(BasePath)
                .XmlSerializer(dontWrapSingleMessages: true) // crucial for AQ
                .UseTransport<ActiveMQ>(() => "Uri = failover:(tcp://localhost:61616,tcp://localhost:61616)?randomize=false&timeout=5000")
                .UnicastBus()
                    .DoNotAutoSubscribe(); //managed by the class Subscriber2Endpoint

           Configure.Instance.DisableSecondLevelRetries();
        }
    }
}
