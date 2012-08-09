namespace NServiceBus.Notifications
{
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using Config;
    using NServiceBus;
    using Satellites;
    using Serialization;

    public class NotificationSatellite : ISatellite
    {
        public IMessageSerializer MessageSerializer { get; set; }

        public void Handle(TransportMessage message)
        {
            SendEmail sendEmail;

            using (var stream = new MemoryStream(message.Body))
                sendEmail = (SendEmail)MessageSerializer.Deserialize(stream).First();

            using (var c = new SmtpClient())
            using (var mailMessage = sendEmail.Message.ToMailMessage())
            {
                c.Send(mailMessage);
            }
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
            get
            {
                return BusExtensions.NotificationAddress;
            }
        }

        public bool Disabled
        {
            get { return ConfigureNotifications.NotificationsDisabled; }
        }
    }
}