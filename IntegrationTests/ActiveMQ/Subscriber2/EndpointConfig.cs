namespace Subscriber2
{
    using NServiceBus;
    using NServiceBus.Features;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public static string BasePath = "..\\..\\..\\storage";
        
        public void Init()
        {
            Configure.Features.Disable<AutoSubscribe>();
            Configure.Features.Disable<SecondLevelRetries>();

            Configure.With()
                //this overrides the NServiceBus default convention of IEvent
                     .CastleWindsorBuilder() // just to show we can mix and match containers
                     .FileShareDataBus(BasePath)
                     .UseTransport<ActiveMQ>(
                         () =>"ServerUrl=failover:(tcp://localhost:61616,tcp://localhost:61616)?randomize=false&timeout=5000");


        }
    }
}
