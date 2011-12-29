namespace NServiceBus
{
    using Persistence.Raven;
    using Raven.Client;
    using Raven.Client.Document;
    using SagaPersisters.Raven;

    public static class ConfigureRavenSagaPersister
    {
        public static Configure RavenSagaPersister(this Configure config)
        {
            if (!config.Configurer.HasComponent<IDocumentStore>())
                config.RavenPersistence();

            config.Configurer.ConfigureComponent<RavenSagaPersister>(DependencyLifecycle.SingleInstance);

            return config;
        }

        public static Configure RavenSagaPersister(this Configure config, string connectionStringName)
        {
            var store = new DocumentStore
            {
                ConnectionStringName = connectionStringName,
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId
            };

            store.Initialize();

            config.Configurer.ConfigureComponent<RavenSagaPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Store, store);

            return config;
        }
    }
}
