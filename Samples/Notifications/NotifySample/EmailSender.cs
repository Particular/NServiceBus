namespace NotifySample
{
    using System;
    using System.Net.Mail;
    using NServiceBus;

    public class EmailSender : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Console.WriteLine("Hit any key to send a email using the notification satellite");

            while (Console.ReadKey().Key.ToString().ToLower() != "q")
                Bus.SendEmail(new MailMessage("X", "Y", "Hello from the the NSB notification support", "Tha body"));
        }

        public void Stop()
        {
            //no-op
        }
    }
}