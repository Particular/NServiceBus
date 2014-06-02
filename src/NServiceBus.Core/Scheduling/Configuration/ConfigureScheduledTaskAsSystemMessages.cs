namespace NServiceBus.Scheduling.Configuration
{
    class ConfigureScheduledTaskAsSystemMessages : IWantToRunBeforeConfiguration
    {
        public void Init(Configure configure)
        {
            MessageConventionExtensions.AddSystemMessagesConventions(t => typeof(Messages.ScheduledTask).IsAssignableFrom(t));
        }
    }
}