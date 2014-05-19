namespace NServiceBus.Scheduling.Configuration
{
    class SchedulerConfiguration : INeedInitialization
    {
        public void Init(Configure config)
        {
            config.Configurer.RegisterSingleton<IScheduledTaskStorage>(new InMemoryScheduledTaskStorage());
            config.Configurer.ConfigureComponent<DefaultScheduler>(DependencyLifecycle.InstancePerCall);
        }
    }
}