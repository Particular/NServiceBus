namespace NServiceBus.Config
{
    using Satellites;

    class SatelliteConfigurer : INeedInitialization
    {
        public void Init(Configure config)
        {
            config.ForAllTypes<ISatellite>(s => Configure.Instance.Configurer.ConfigureComponent(s, DependencyLifecycle.SingleInstance));
        }
    }
}