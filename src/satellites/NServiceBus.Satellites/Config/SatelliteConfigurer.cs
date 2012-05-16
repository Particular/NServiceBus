using System;
using System.Linq;
using NServiceBus.Config;

namespace NServiceBus.Satellites.Config
{
    public class SatelliteConfigurer : INeedInitialization, IWantToRunBeforeConfigurationIsFinalized
    {
        public void Init()
        {
            Configure.Instance.ForAllTypes<ISatellite>(s => Configure.Instance.Configurer.ConfigureComponent(s, DependencyLifecycle.SingleInstance));
        }

        public void Run()
        {                                              
            Configure.Instance.Builder
                .BuildAll<ISatellite>()
                .ToList()
                .ForEach(s =>
                            {
                                var satelliteConf = GetSatelliteConfiguration(s);

                                SatelliteLauncher.Satellites.Add(new SatelliteContext
                                                                    {
                                                                        TypeOfSatellite = s.GetType(),
                                                                        NumberOfWorkerThreads = satelliteConf != null ? satelliteConf.NumberOfWorkerThreads : 0,
                                                                        MaxRetries = satelliteConf != null ? satelliteConf.MaxRetries : 0,
                                                                        IsTransactional = satelliteConf != null ? satelliteConf.IsTransactional : true,
                                                                        Enabled = satelliteConf != null ? satelliteConf.Enabled : true
                                                                    });
                            });
        }

        static SatelliteConfigurationElement GetSatelliteConfiguration(ISatellite satellite)
        {
            var configSection = Configure.GetConfigSection<SatelliteConfig>();

            if (configSection == null)
                return null;

            try
            {
                return configSection.Satellites
                    .Cast<SatelliteConfigurationElement>()
                    .Where(s => s.Name == satellite.GetType().Name || s.Name == satellite.GetType().Name.Replace("Satellite", ""))
                    .SingleOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}