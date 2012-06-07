namespace NServiceBus.Notifications
{
    using NServiceBus;

    public class RegisterSystemMessages:IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            MessageConventionExtensions.AddSystemMessagesConventions(t=>typeof(SendEmail) == t);
        }

        
    }
}