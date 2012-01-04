using NServiceBus;

namespace Headquarter
{
    using System;
    using Messages;

    public class HeadquarterService : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {

            Console.WriteLine("Press 'Enter' to send a message to SiteA and SiteB, SiteA will also reply to the sent message. To exit, Ctrl + C");

            while (Console.ReadLine() != null)
            {
                Bus.SendToSites(new[] { "SiteA", "SiteB" }, new PriceUpdated
                {
                    ProductId = 2,
                    NewPrice = 100.0,
                    ValidFrom = DateTime.Today
                });

                Console.WriteLine("Message sent, check the output in the remote sites");
            }

        }

        public void Stop()
        {

        }
    }
}
