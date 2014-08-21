namespace NServiceBus.Config
{
    using Satellites;

    class SatelliteConfigurer : INeedInitialization
    {
        public void Customize(BusConfiguration builder)
        {
            Configure.ForAllTypes<ISatellite>(builder.Settings.GetAvailableTypes(), t => builder.RegisterComponents(c => c.ConfigureComponent(t, DependencyLifecycle.SingleInstance)));
        }
    }
}