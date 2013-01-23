namespace Subscriber1
{
    using NServiceBus;

    class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public static string BasePath = "..\\..\\..\\storage";

        public void Init()
        {
            var config = Configure.With()
                .DefaultBuilder()
                .PurgeOnStartup(true)
                .FileShareDataBus(BasePath)
                .UseTransport<ActiveMQ>(() => "activemq:tcp://localhost:61616")
                .XmlSerializer(dontWrapSingleMessages: true);

            Configure.Instance.DisableSecondLevelRetries();
        }
    }
}