namespace NServiceBus
{
    using ObjectBuilder;
    using Config;
    using Gateway.Persistence;
    using Gateway.Persistence.Raven;
    using Raven.Client;

    class GatewayDefaults : IWantToRunWhenConfigurationIsComplete
    {
        public IConfigureComponents Configurer { get; set; }
        public void Run()
        {
            if (!Configurer.HasComponent<IPersistMessages>())
            {
                if (!Configurer.HasComponent<IDocumentStore>())
                    Configure.Instance.RavenPersistence();
                Configurer.ConfigureComponent<RavenDBPersistence>(DependencyLifecycle.InstancePerCall);
            }
                    
        }
    }
}