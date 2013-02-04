﻿namespace MyPublisher
{
    using NServiceBus;

    class EndpointConfig :  IConfigureThisEndpoint, AsA_Publisher,IWantCustomInitialization
    {
        public static string BasePath = "..\\..\\..\\storage";

        public void Init()
        {
            var config = Configure.With()
                //this overrides the NServiceBus default convention of IEvent
                .DefaultBuilder()
                .FileShareDataBus(BasePath)
                .UseTransport<ActiveMQ>(() => "failover:(tcp://localhost:61616,tcp://localhost:61616)?transport.randomize=false&transport.timeout=5000")
                .XmlSerializer(dontWrapSingleMessages: true);

            Configure.Instance.DisableSecondLevelRetries();
        }
    }
}
