namespace NServiceBus.Notifications
{
    /// <summary>
    /// Registers send email notifications.
    /// </summary>
    public class RegisterSystemMessages : IWantToRunBeforeConfiguration
    {
        /// <summary>
        /// Invoked before configuration starts.
        /// </summary>
        public void Init()
        {
            MessageConventionExtensions.AddSystemMessagesConventions(t => typeof (SendEmail) == t);
        }
    }
}