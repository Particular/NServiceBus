namespace NServiceBus.Notifications
{
    /// <summary>
    /// Send email wrapper class to be used interrnally when sending an email using Bus.SendEmail().
    /// </summary>
    public class SendEmail
    {
        /// <summary>
        /// The <see cref="MailMessage"/>.
        /// </summary>
        public MailMessage Message { get; set; }
    }
}