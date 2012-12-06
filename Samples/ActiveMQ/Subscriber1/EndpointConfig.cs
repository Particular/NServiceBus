namespace Subscriber1
{
    using NServiceBus;
    using NServiceBus.ActiveMQ.Config;

    class EndpointConfig : IConfigureThisEndpoint, AsA_Server,IWantCustomInitialization
    {
        public void Init()
        {
            var config = Configure.With()
                //this overrides the NServiceBus default convention of IEvent
                .DefaultBuilder()
                
                .ActiveMqTransport("B", "activemq:tcp://localhost:61616")
                .XmlSerializer(dontWrapSingleMessages: true)
//                .UnicastBus()
//                    .PurgeOnStartup(true)
;

            Configure.Instance.DisableSecondLevelRetries();
        }
    }
}