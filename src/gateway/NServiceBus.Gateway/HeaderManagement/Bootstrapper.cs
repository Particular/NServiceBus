namespace NServiceBus.Gateway.HeaderManagement
{
    using NServiceBus.Config;
    using NServiceBus.ObjectBuilder;

    public class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<GatewayHeaderManager>(
                DependencyLifecycle.SingleInstance);
        }
    }
}