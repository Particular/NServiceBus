namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Net.Mail;
    using System.Text;

    /// <summary>
    /// Represents an e-mail message that can be sent using the <see cref="IBus"/>.
    /// </summary>
    [Serializable]
    public class MailMessage
    {
        /// <summary>
        /// Initializes an empty instance of the <see cref="MailMessage"/> class.
        /// </summary>
        public MailMessage()
        {
            Bcc = new List<string>();
            CC = new List<string>();
            ReplyToList = new List<string>();
            To = new List<string>();
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MailMessage"/> class by using the specified <see cref="T:System.String"/> class objects.
        /// </summary>
        /// <param name="from">A <see cref="T:System.String"/> that contains the address of the sender of the e-mail message.</param>
        /// <param name="to">A <see cref="T:System.String"/> that contains the addresses of the recipients of the e-mail message.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="from"/> is null.-or-<paramref name="to"/> is null.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="from"/> is <see cref="F:System.String.Empty"/> ("").-or-<paramref name="to"/> is <see cref="F:System.String.Empty"/> ("").</exception>
        public MailMessage(string from, string to) : this()
        {
            if (from == null)
                throw new ArgumentNullException("from");

            if (to == null)
                throw new ArgumentNullException("to");

            if (from == String.Empty)
                throw new ArgumentException("The parameter 'from' cannot be an empty string.", "from");

            if (to == String.Empty)
                throw new ArgumentException("The parameter 'to' cannot be an empty string.", "to");

            To.Add(to);
            From = from;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Net.Mail.MailMessage"/> class.
        /// </summary>
        /// <param name="from">A <see cref="T:System.String"/> that contains the address of the sender of the e-mail message.</param>
        /// <param name="to">A <see cref="T:System.String"/> that contains the address of the recipient of the e-mail message.</param>
        /// <param name="subject">A <see cref="T:System.String"/> that contains the subject text.</param>
        /// <param name="body">A <see cref="T:System.String"/> that contains the message body.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="from"/> is null.-or-<paramref name="to"/> is null.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="from"/> is <see cref="F:System.String.Empty"/> ("").-or-<paramref name="to"/> is <see cref="F:System.String.Empty"/> ("").</exception>
        public MailMessage(string from, string to, string subject, string body) : this(from, to)
        {
            Subject = subject;
            Body = body;
        }

        /// <summary>
        /// Gets the address collection that contains the blind carbon copy (BCC) recipients for this e-mail message.
        /// </summary>
        /// 
        /// <returns>
        /// A writable <see cref="List{T}"/> object.
        /// </returns>
        public List<string> Bcc { get; set; }

        /// <summary>
        /// Gets or sets the message body.
        /// </summary>
        /// 
        /// <returns>
        /// A <see cref="T:System.String"/> value that contains the body text.
        /// </returns>
        public string Body { get; set; }

        /// <summary>
        /// Gets the address collection that contains the carbon copy (CC) recipients for this e-mail message.
        /// </summary>
        /// 
        /// <returns>
        /// A writable <see cref="List{String}"/> object.
        /// </returns>
        public List<string> CC { get; set; }

        /// <summary>
        /// Gets or sets the encoding used to encode the message body.
        /// </summary>
        /// 
        /// <returns>
        /// An <see cref="T:System.Text.Encoding"/> applied to the contents of the <see cref="P:System.Net.Mail.MailMessage.Body"/>.
        /// </returns>
        public Encoding BodyEncoding { get; set; }

        /// <summary>
        /// Gets or sets the delivery notifications for this e-mail message.
        /// </summary>
        /// 
        /// <returns>
        /// A <see cref="T:System.Net.Mail.DeliveryNotificationOptions"/> value that contains the delivery notifications for this message.
        /// </returns>
        public DeliveryNotificationOptions DeliveryNotificationOptions { get; set; }

        /// <summary>
        /// Gets or sets the from address for this e-mail message.
        /// </summary>
        /// 
        /// <returns>
        /// The from address information.
        /// </returns>
        public string From { get; set; }

        /// <summary>
        /// Gets the e-mail headers that are transmitted with this e-mail message.
        /// </summary>
        /// 
        /// <returns>
        /// A <see cref="Dictionary{String, String}"/> that contains the e-mail headers.
        /// </returns>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Gets or sets the encoding used for the user-defined custom headers for this e-mail message.
        /// </summary>
        /// 
        /// <returns>
        /// The encoding used for user-defined custom headers for this e-mail message.
        /// </returns>
        public Encoding HeadersEncoding { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the mail message body is in Html.
        /// </summary>
        /// 
        /// <returns>
        /// true if the message body is in Html; else false. The default is false.
        /// </returns>
        public bool IsBodyHtml { get; set; }

        /// <summary>
        /// Gets or sets the list of addresses to reply to for the mail message.
        /// </summary>
        /// 
        /// <returns>
        /// The list of the addresses to reply to for the mail message.
        /// </returns>
        public List<string> ReplyToList { get; set; }

        /// <summary>
        /// Gets or sets the sender's address for this e-mail message.
        /// </summary>
        /// 
        /// <returns>
        /// The sender's address information.
        /// </returns>
        public string Sender { get; set; }

        /// <summary>
        /// Gets or sets the subject line for this e-mail message.
        /// </summary>
        /// 
        /// <returns>
        /// A <see cref="T:System.String"/> that contains the subject content.
        /// </returns>
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the encoding used for the subject content for this e-mail message.
        /// </summary>
        /// 
        /// <returns>
        /// An <see cref="T:System.Text.Encoding"/> that was used to encode the <see cref="MailMessage.Subject"/> property.
        /// </returns>
        public Encoding SubjectEncoding { get; set; }

        /// <summary>
        /// Gets the address collection that contains the recipients of this e-mail message.
        /// </summary>
        /// 
        /// <returns>
        /// A writable <see cref="List{String}"/> object.
        /// </returns>
        public List<string> To { get; set; }

        /// <summary>
        /// Gets or sets the priority of this e-mail message.
        /// </summary>
        /// 
        /// <returns>
        /// A <see cref="T:System.Net.Mail.MailPriority"/> that contains the priority of this message.
        /// </returns>
        public MailPriority Priority { get; set; }
        

        /// <summary>
        /// Converts <see cref="MailMessage"/> to <see cref="System.Net.Mail.MailMessage"/>.
        /// </summary>
        /// <returns>A <see cref="System.Net.Mail.MailMessage"/>.</returns>
        internal System.Net.Mail.MailMessage ToMailMessage()
        {
            var mail = new System.Net.Mail.MailMessage();
            if (From != null)
                mail.From = new MailAddress(From);
            To.ForEach(a => mail.To.Add(new MailAddress(a)));
            ReplyToList.ForEach(a => mail.ReplyToList.Add(new MailAddress(a)));
            Bcc.ForEach(a => mail.Bcc.Add(new MailAddress(a)));
            CC.ForEach(a => mail.CC.Add(new MailAddress(a)));

            foreach (var header in Headers)
            {
                mail.Headers[header.Key] = header.Value;
            }

            mail.Body = Body;
            if (BodyEncoding != null)
                mail.BodyEncoding = BodyEncoding;
            mail.DeliveryNotificationOptions = DeliveryNotificationOptions;
            mail.IsBodyHtml = IsBodyHtml;
            if (Sender != null)
                mail.Sender = new MailAddress(Sender);
            mail.Subject = Subject;
            if (SubjectEncoding != null)
                mail.SubjectEncoding = SubjectEncoding;

            mail.Priority = Priority;

            return mail;
        }
    }
}