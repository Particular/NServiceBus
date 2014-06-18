namespace NServiceBus.Scheduling.Configuration
{
    class ConfigureScheduledTaskAsSystemMessages : INeedInitialization
    {
        public void Init(Configure config)
        {
            config.Settings.Get<Conventions>().AddSystemMessagesConventions(t => typeof(Messages.ScheduledTask).IsAssignableFrom(t));
        }
    }
}