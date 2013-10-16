namespace NServiceBus.Scheduling.Configuration
{
    public class ConfigureScheduledTaskAsSystemMessages : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            Configure.Instance.AddSystemMessagesAs(t => typeof(Messages.ScheduledTask).IsAssignableFrom(t));
        }
    }
}