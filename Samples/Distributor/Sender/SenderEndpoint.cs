namespace Sender
{
    using System;
    using MyMessages;
    using NServiceBus;

    public class SenderEndpoint : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Console.WriteLine("Press 'Enter' to send a message.To exit, Ctrl + C");

            while (Console.ReadLine() != null)
            {
               
                Bus.Send(new MessageToBeDistributed());

                Console.WriteLine("Message sent");
                Console.WriteLine("==========================================================================");

            }
        }

        public void Stop()
        {

        }
    }
}