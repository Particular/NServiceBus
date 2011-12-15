
namespace NServiceBus
{
    using ObjectBuilder;
    using Config;
    using Gateway.Persistence;
    using Gateway.Persistence.Raven;

    class GatewayDefaults : IWantToRunWhenConfigurationIsComplete
    {
        public IConfigureComponents Configurer { get; set; }
        public void Run()
        {
            if (!Configurer.HasComponent<IPersistMessages>())
                Configurer.ConfigureComponent<RavenDBPersistence>(DependencyLifecycle.InstancePerCall);
                    
        }
    }
}