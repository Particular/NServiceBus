namespace MyPublisher
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
                .ActiveMQTransport(() => "activemq:tcp://localhost:61616")
                .XmlSerializer(dontWrapSingleMessages: true);
        }
    }
}
