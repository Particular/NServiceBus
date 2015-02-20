namespace NServiceBus.Config
{
    using Satellites;

    //todo: make this a feature
    class SatelliteConfigurer : INeedInitialization
    {
        public void Customize(BusConfiguration configuration)
        {
            Configure.ForAllTypes<ISatellite>(configuration.Settings.GetAvailableTypes(), t => configuration.RegisterComponents(c => c.ConfigureComponent(t, DependencyLifecycle.SingleInstance)));

            //need to register it here since we're hacking the main pipeline
            configuration.RegisterComponents(c => c.ConfigureComponent<ExecuteSatelliteHandlerBehavior>(DependencyLifecycle.InstancePerCall));
        }
    }
}