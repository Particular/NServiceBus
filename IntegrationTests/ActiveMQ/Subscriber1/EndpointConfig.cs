namespace Subscriber1
{
    using NServiceBus;
    using NServiceBus.Features;

    class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public static string BasePath = "..\\..\\..\\storage";

        public void Init()
        {
            var config = Configure.With()
                                  .DefaultBuilder()
                                  .PurgeOnStartup(true)
                                  .FileShareDataBus(BasePath)
                                  .UseTransport<ActiveMQ>(() =>"ServerUrl=failover:(tcp://localhost:61616,tcp://localhost:61616)?randomize=false&timeout=5000");

            Configure.Features.Disable<SecondLevelRetries>();
        }
    }
}