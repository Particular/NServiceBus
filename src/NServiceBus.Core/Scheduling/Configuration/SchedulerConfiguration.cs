namespace NServiceBus.Scheduling.Configuration
{
    public class SchedulerConfiguration : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.RegisterSingleton<IScheduledTaskStorage>(new InMemoryScheduledTaskStorage());
            Configure.Instance.Configurer.ConfigureComponent<DefaultScheduler>(DependencyLifecycle.InstancePerCall);
        }
    }
}