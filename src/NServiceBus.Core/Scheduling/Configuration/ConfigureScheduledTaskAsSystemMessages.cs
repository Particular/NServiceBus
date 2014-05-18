namespace NServiceBus.Scheduling.Configuration
{
    public class ConfigureScheduledTaskAsSystemMessages : IWantToRunBeforeConfiguration
    {
        public void Init(Configure configure)
        {
            MessageConventionExtensions.AddSystemMessagesConventions(t => typeof(Messages.ScheduledTask).IsAssignableFrom(t));
        }
    }
}