namespace NServiceBus.Gateway.HeaderManagement
{
    public class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<GatewayHeaderManager>(
                DependencyLifecycle.SingleInstance);
        }
    }
}