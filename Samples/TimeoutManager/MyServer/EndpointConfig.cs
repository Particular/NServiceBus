namespace MyServer
{
    using NServiceBus;
    using NServiceBus.Timeout.Hosting.Windows.Config;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                .UseTimeoutManager();
        }
    }
}
