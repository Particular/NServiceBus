namespace MyPublisher
{
    using System;
    using System.Net.Mail;

    using NServiceBus.Notifications;

    public class ConsoleMailer : ISendMail
    {
        public void Send(MailMessage message)
        {
            Console.WriteLine("New email from {0} to {1}.", message.From, message.To);
        }
    }
}