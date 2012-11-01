namespace NServiceBus.Config
{
    using Satellites;

    public class SatelliteLauncherConfiguration : NServiceBus.INeedInitialization
    {                
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<SatelliteTransportBuilder>(DependencyLifecycle.SingleInstance);                      
            Configure.Instance.Configurer.ConfigureComponent<SatelliteLauncher>(DependencyLifecycle.SingleInstance);            
        }
    }
}