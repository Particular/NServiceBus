namespace NServiceBus
{
    using Persistence.Raven;
    using Raven.Client;
    using SagaPersisters.Raven;

    public static class ConfigureRavenSagaPersister
    {
        public static Configure RavenSagaPersister(this Configure config)
        {
            if (!Sagas.Impl.Configure.SagasWereFound)
            {
                return config;
            }

            if (!config.Configurer.HasComponent<StoreAccessor>())
            {
                config.RavenPersistence();
            }

            config.Configurer.ConfigureComponent<RavenSagaPersister>(DependencyLifecycle.InstancePerCall);

            return config;
        }
    }
}
