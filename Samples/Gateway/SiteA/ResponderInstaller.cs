using NServiceBus;

namespace SiteA
{
    class ResponderInstaller : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<CustomHttpResponder>(DependencyLifecycle.InstancePerCall);
        }
    }
}