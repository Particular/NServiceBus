namespace NServiceBus.Config
{
    using Satellites;

    class SatelliteConfigurer : INeedInitialization
    {
        public void Init(Configure config)
        {
            config.ForAllTypes<ISatellite>(s => config.Configurer.ConfigureComponent(s, DependencyLifecycle.SingleInstance));
        }
    }
}