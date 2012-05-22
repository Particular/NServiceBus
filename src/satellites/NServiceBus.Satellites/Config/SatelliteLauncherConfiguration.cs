﻿using NServiceBus.Satellites;

namespace NServiceBus.Config
{
    public class SatelliteLauncherConfiguration : INeedInitialization
    {                
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<SatelliteTransportBuilder>(DependencyLifecycle.SingleInstance);                      
            Configure.Instance.Configurer.ConfigureComponent<SatelliteLauncher>(DependencyLifecycle.SingleInstance);            
        }
    }
}