namespace NServiceBus.Notifications
{
    /// <summary>
    /// Send email wrapper class to be used internally when sending an email using Bus.SendEmail().
    /// </summary>
    internal class SendEmail
    {
        /// <summary>
        /// The <see cref="MailMessage"/>.
        /// </summary>
        public MailMessage Message { get; set; }
    }
}