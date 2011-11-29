namespace MyServer
{
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                .UseTimeoutManager();
        }
    }
}
