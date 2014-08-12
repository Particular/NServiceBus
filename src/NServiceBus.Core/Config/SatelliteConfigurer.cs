namespace NServiceBus.Config
{
    using Satellites;

    class SatelliteConfigurer : IConfigureBus
    {
        public void Customize(ConfigurationBuilder builder)
        {
            Configure.ForAllTypes<ISatellite>(builder.settings.GetAvailableTypes(), t => builder.RegisterComponents(c => c.ConfigureComponent(t, DependencyLifecycle.SingleInstance)));
        }
    }
}