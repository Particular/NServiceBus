namespace SiteA
{
    using NServiceBus;

    // The endpoint is started with the RunGateway profile which turns it on. The Lite profile is also
    // active which will configure the persistence to be InMemory
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server,IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                .FileShareDataBus(".\\databus");
        }
    }
}
