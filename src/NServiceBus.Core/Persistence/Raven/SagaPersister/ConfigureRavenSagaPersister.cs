namespace NServiceBus
{
    using Persistence.Raven;
    using Persistence.Raven.SagaPersister;

    public static class ConfigureRavenSagaPersister_Obsolete
    {
        public static Configure RavenSagaPersister(this Configure config)
        {
            if (!config.Configurer.HasComponent<StoreAccessor>())
            {
                config.RavenPersistence();
            }

            config.Configurer.ConfigureComponent<RavenSagaPersister>(DependencyLifecycle.InstancePerCall);

            return config;
        }
    }
}
