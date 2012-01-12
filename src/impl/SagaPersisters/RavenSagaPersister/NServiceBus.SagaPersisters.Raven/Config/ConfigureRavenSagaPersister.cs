namespace NServiceBus
{
    using Raven.Client;
    using SagaPersisters.Raven;

    public static class ConfigureRavenSagaPersister
    {
        public static Configure RavenSagaPersister(this Configure config)
        {
            if (!config.Configurer.HasComponent<IDocumentStore>())
                config.RavenPersistence();

            config.Configurer.ConfigureComponent<RavenSagaPersister>(DependencyLifecycle.InstancePerCall);

            return config;
        }
    }
}
