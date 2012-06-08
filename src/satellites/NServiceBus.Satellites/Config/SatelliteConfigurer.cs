using NServiceBus.Satellites;

namespace NServiceBus.Config
{
    public class SatelliteConfigurer : NServiceBus.INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.ForAllTypes<ISatellite>(s => Configure.Instance.Configurer.ConfigureComponent(s, DependencyLifecycle.SingleInstance));
        }
    }
}