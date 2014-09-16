namespace NServiceBus.Config
{
    using Satellites;

    class SatelliteConfigurer : INeedInitialization
    {
        public void Customize(BusConfiguration configuration)
        {
            Configure.ForAllTypes<ISatellite>(configuration.Settings.GetAvailableTypes(), t => configuration.RegisterComponents(c => c.ConfigureComponent(t, DependencyLifecycle.SingleInstance)));
        }
    }
}