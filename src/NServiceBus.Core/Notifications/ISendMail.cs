namespace NServiceBus.Notifications
{
    using System.Net.Mail;

    public interface ISendMail
    {
        void Send(MailMessage message);
    }
}