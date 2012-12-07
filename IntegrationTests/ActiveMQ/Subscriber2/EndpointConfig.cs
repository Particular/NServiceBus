namespace Subscriber2
{
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            var config = Configure.With()
                //this overrides the NServiceBus default convention of IEvent
                .CastleWindsorBuilder() // just to show we can mix and match containers
                .XmlSerializer(dontWrapSingleMessages: true) // crucial for AQ
                .ActiveMqTransport("activemq:tcp://localhost:61616")
                .UnicastBus()
                    .DoNotAutoSubscribe(); //managed by the class Subscriber2Endpoint

           Configure.Instance.DisableSecondLevelRetries();
        }
    }
}
