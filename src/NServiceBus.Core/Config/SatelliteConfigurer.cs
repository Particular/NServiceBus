namespace NServiceBus.Config
{
    using Satellites;

    class SatelliteConfigurer : INeedInitialization
    {
        public void Customize(ConfigurationBuilder builder)
        {
            Configure.ForAllTypes<ISatellite>(builder.settings.GetAvailableTypes(), t => builder.RegisterComponents(c => c.ConfigureComponent(t, DependencyLifecycle.SingleInstance)));
        }
    }
}