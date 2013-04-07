namespace NServiceBus
{
    using Persistence.Raven;
    using Persistence.Raven.SagaPersister;
    using Raven.Client;

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
