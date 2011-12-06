namespace NServiceBus.Timeout.Hosting.Windows.Config
{
    using NServiceBus.Config;
    using ObjectBuilder;
    using Core;
    using Persistence;

    class TimeoutManagerDefaults : IWantToRunWhenConfigurationIsComplete
    {
        public IConfigureComponents Configurer { get; set; }
        public void Run()
        {
            if (ConfigureTimeoutManager.TimeoutManagerEnabled && !Configurer.HasComponent<IPersistTimeouts>())
                Configurer.ConfigureComponent<RavenTimeoutPersistence>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.Database, Configure.EndpointName);
        }
    }
}