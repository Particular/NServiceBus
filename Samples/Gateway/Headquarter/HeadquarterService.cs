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

            Console.WriteLine("Press 'A' to send a message to SiteA. To exit, Ctrl + C");

            string key;

            while ((key = Console.ReadLine()) != null)
            {
                if (key.ToLower() == "a")

                    //todo - use a sitekey instead when we have support for that
                    Bus.SendToSites(new[] { "http://localhost:8080/siteA" }, new PriceUpdated
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
