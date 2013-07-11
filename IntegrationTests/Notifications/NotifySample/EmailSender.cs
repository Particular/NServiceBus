namespace NotifySample
{
    using System;
    using NServiceBus;

    public class EmailSender : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }

        public void Start()
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