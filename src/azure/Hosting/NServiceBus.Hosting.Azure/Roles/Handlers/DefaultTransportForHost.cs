namespace NServiceBus.Hosting.Azure.Roles.Handlers
{
    using Transports;

    public class DefaultTransportForHost : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {

            if (Configure.Instance.Configurer.HasComponent<ISendMessages>())
            {
                return;
            }

            Configure.Instance.UseTransport<WindowsAzureStorage>();
        }
    }
}