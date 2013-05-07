namespace NServiceBus.Notifications
{
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using Satellites;
    using Serialization;

    /// <summary>
    /// Satellite implementation to handle <see cref="SendEmail"/> messages.
    /// </summary>
    public class NotificationSatellite : ISatellite
    {
        private readonly IMessageSerializer messageSerializer;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="messageSerializer"></param>
        public NotificationSatellite(IMessageSerializer messageSerializer)
        {
            this.messageSerializer = messageSerializer;
        }

        /// <summary>
        /// This method is called when a message is available to be processed.
        /// </summary>
        /// <param name="message">The <see cref="TransportMessage"/> received.</param>
        public bool Handle(TransportMessage message)
        {
            SendEmail sendEmail;

            using (var stream = new MemoryStream(message.Body))
            {
                sendEmail = (SendEmail)messageSerializer.Deserialize(stream, new[] { typeof(SendEmail) }).First();
            }

            using (var c = new SmtpClient())
            using (var mailMessage = sendEmail.Message.ToMailMessage())
            {
                c.Send(mailMessage);
            }

            return true;
        }

        /// <summary>
        /// Starts the <see cref="ISatellite"/>.
        /// </summary>
        public void Start()
        {
            //no-op
        }

        /// <summary>
        /// Stops the <see cref="ISatellite"/>.
        /// </summary>
        public void Stop()
        {
            //no-op
        }

        /// <summary>
        /// The <see cref="Address"/> for this <see cref="ISatellite"/> to use when receiving messages.
        /// </summary>
        public Address InputAddress
        {
            get { return Configure.Instance.GetMasterNodeAddress().SubScope("Notifications"); }
        }

        /// <summary>
        /// Set to <code>true</code> to disable this <see cref="ISatellite"/>.
        /// </summary>
        public bool Disabled
        {
            get
            {
                if (Configure.Instance.GetMasterNodeAddress() != Address.Local)
                    return false;
                return ConfigureNotifications.NotificationsDisabled;
            }
        }
    }
}