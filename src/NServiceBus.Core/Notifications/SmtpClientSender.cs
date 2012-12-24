namespace NServiceBus.Notifications
{
    using System.Net.Mail;

    class SmtpClientSender : ISendMail
    {
        public void Send(MailMessage message)
        {
            using (var c = new SmtpClient())
            {
                c.Send(message);
            }
        }
    }
}