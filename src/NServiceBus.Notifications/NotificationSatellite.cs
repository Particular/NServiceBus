namespace NServiceBus.Notifications
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using System.Text;
    using Features;
    using Logging;
    using Satellites;
    using Serialization;
    using Settings;

    /// <summary>
    ///     Satellite implementation to handle <see cref="SendEmail" /> messages.
    /// </summary>
    public class NotificationSatellite : ISatellite
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="messageSerializer"></param>
        public NotificationSatellite(IMessageSerializer messageSerializer)
        {
            this.messageSerializer = messageSerializer;
            address = Configure.Instance.GetMasterNodeAddress().SubScope("Notifications");
        }

        /// <summary>
        ///     This method is called when a message is available to be processed.
        /// </summary>
        /// <param name="message">
        ///     The <see cref="TransportMessage" /> received.
        /// </param>
        public bool Handle(TransportMessage message)
        {
            SendEmail sendEmail;

            using (var stream = new MemoryStream(message.Body))
            {
                sendEmail = (SendEmail) messageSerializer.Deserialize(stream, new[] {typeof (SendEmail)}).First();
            }

            using (var c = new SmtpClient())
            {
                using (var mailMessage = sendEmail.Message.ToMailMessage())
                {
                    try
                    {
                        c.Send(mailMessage);
                    }
                    catch (SmtpFailedRecipientsException ex)
                    {
                        var originalRecipientCount = mailMessage.To.Count + mailMessage.Bcc.Count + mailMessage.CC.Count;
                        if (ex.InnerExceptions.Length == originalRecipientCount)
                        {
                            // All messages failed.
                            // FLR/SLR can handle it.
                            throw;
                        }

                        var sb = new StringBuilder();

                        foreach (var recipientException in ex.InnerExceptions)
                        {
                            sb.AppendLine();
                            sb.AppendFormat("{0}", recipientException.FailedRecipient);
                        }

                        Logger.WarnFormat(
                            "NServiceBus failed to send an email to some of its recipients, here is the list of recipients that failed:{0}", sb.ToString());

                        deliveryFailuresCallback(ex);
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     Starts the <see cref="ISatellite" />.
        /// </summary>
        public void Start()
        {
            deliveryFailuresCallback = SettingsHolder.Get<Action<SmtpFailedRecipientsException>>("Notifications.DeliveryFailuresCallback");
        }

        /// <summary>
        ///     Stops the <see cref="ISatellite" />.
        /// </summary>
        public void Stop()
        {
            //no-op
        }

        /// <summary>
        ///     The <see cref="Address" /> for this <see cref="ISatellite" /> to use when receiving messages.
        /// </summary>
        public Address InputAddress
        {
            get { return address; }
        }

        /// <summary>
        ///     Set to <code>true</code> to disable this <see cref="ISatellite" />.
        /// </summary>
        public bool Disabled
        {
            get { return !Feature.IsEnabled<Notifications>(); }
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof (NotificationSatellite));

        readonly Address address;
        readonly IMessageSerializer messageSerializer;
        Action<SmtpFailedRecipientsException> deliveryFailuresCallback;
    }
}