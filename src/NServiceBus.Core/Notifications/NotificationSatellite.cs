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

        public NotificationSatellite(IMessageSerializer messageSerializer)
        {
            this.messageSerializer = messageSerializer;
        }

        public bool Handle(TransportMessage message)
        {
            SendEmail sendEmail;

            using (var stream = new MemoryStream(message.Body))
            {
                sendEmail = (SendEmail)messageSerializer.Deserialize(stream, new[] { typeof(SendEmail).FullName }).First();
            }

            using (var c = new SmtpClient())
            using (var mailMessage = sendEmail.Message.ToMailMessage())
            {
                c.Send(mailMessage);
            }

            return true;
        }

        public void Start()
        {
            //no-op
        }

        public void Stop()
        {
            //no-op
        }

        public Address InputAddress
        {
            get { return Configure.Instance.GetMasterNodeAddress().SubScope("Notifications"); }
        }

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