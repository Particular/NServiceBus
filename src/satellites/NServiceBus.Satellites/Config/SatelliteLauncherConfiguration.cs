using NServiceBus.Config;

namespace NServiceBus.Satellites.Config
{
    public class SatelliteLauncherConfiguration : INeedInitialization
    {                
        public void Init()
        {
            Configure.Instance.Configurer.RegisterSingleton<ISatelliteTransportBuilder>(new SatelliteTransportBuilder());                      
            Configure.Instance.Configurer.ConfigureComponent<SatelliteLauncher>(DependencyLifecycle.SingleInstance);            
        }
    }
}