namespace Server
{
    using NServiceBus;

    public class EndpointConfig:IConfigureThisEndpoint,AsA_Publisher,IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"));
        }
    }
}
