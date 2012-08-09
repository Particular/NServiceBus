namespace NServiceBus.Notifications
{
    using System;
    using System.Collections.Generic;
    using System.Net.Mail;
    using System.Text;

    [Serializable]
    public class MailMessage 
    {
        public MailMessage()
        {
            Bcc = new List<string>();
            CC = new List<string>();
            ReplyToList = new List<string>();
            To = new List<string>();
            Headers = new Dictionary<string, string>();
        }

        public List<string> Bcc { get; set; }
        public string Body { get; set; }
        public List<string> CC { get; set; }
        public int? BodyEncoding { get; set; }
        public DeliveryNotificationOptions DeliveryNotificationOptions { get; set; }
        public string From { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public bool IsBodyHtml { get; set; }
        public List<string> ReplyToList { get; set; }
        public string Sender { get; set; }
        public string Subject { get; set; }
        public int? SubjectEncoding { get; set; }
        public List<string> To { get; set; }

        public System.Net.Mail.MailMessage ToMailMessage()
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
                mail.BodyEncoding = Encoding.GetEncoding(BodyEncoding.Value);
            mail.DeliveryNotificationOptions = DeliveryNotificationOptions;
            mail.IsBodyHtml = IsBodyHtml;
            if (Sender != null)
                mail.Sender = new MailAddress(Sender);
            mail.Subject = Subject;
            if (SubjectEncoding != null)
                mail.SubjectEncoding = Encoding.GetEncoding(SubjectEncoding.Value);
            return mail;
        }
    }
}