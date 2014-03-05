namespace NServiceBus.Scheduling.Configuration
{
    public class ConfigureScheduledTaskAsSystemMessages : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            MessageConventionExtensions.AddSystemMessagesConventions(t => typeof(Messages.ScheduledTask).IsAssignableFrom(t));
        }
    }
}