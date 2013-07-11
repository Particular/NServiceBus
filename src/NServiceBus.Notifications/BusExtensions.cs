namespace NServiceBus
{
    using System;
    using Features;
    using Notifications;

    /// <summary>
    /// Extension methods for notifications
    /// </summary>
    public static class BusExtensions
    {
        /// <summary>
        /// Sends the specified message via the <see cref="IBus"/> to an SMTP server for delivery.
        /// </summary>
        /// <param name="bus">The <see cref="IBus"/> that is sending the message.</param>
        /// <param name="message">The <see cref="MailMessage"/> to send.</param>
        public static void SendEmail(this IBus bus, MailMessage message)
        {
            if (!Feature.IsEnabled<Features.Notifications>())
                throw new InvalidOperationException("Notifications feature is disabled. You need to ensure that this feature is enabled.");

            bus.Send(Configure.Instance.GetMasterNodeAddress().SubScope("Notifications"), new SendEmail
                                             {
                                                 Message = message
                                             });
        }
    }
}