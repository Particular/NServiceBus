namespace NServiceBus.Config
{
    using Satellites;

    public class SatelliteConfigurer : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.ForAllTypes<ISatellite>(s => Configure.Instance.Configurer.ConfigureComponent(s, DependencyLifecycle.SingleInstance));
        }
    }
}