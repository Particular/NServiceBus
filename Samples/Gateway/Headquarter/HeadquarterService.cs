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

            Console.WriteLine("Press 'A' to send a message to SiteA and SiteB, SiteA will also reply to the sent message. To exit, Ctrl + C");

            string key;

            while ((key = Console.ReadLine()) != null)
            {
                if (key.ToLower() == "a")
                {
                    //todo - use a sitekey instead when we have support for that
                    Bus.SendToSites(new[] { "http://localhost:8085/siteA", "http://localhost:8085/siteB" }, new PriceUpdated
                    {
                        ProductId = 2,
                        NewPrice = 100.0,
                        ValidFrom = DateTime.Today
                    });

                    Console.WriteLine("Message sent, check the output in the remote sites");
                    continue;
                }

                Console.WriteLine("Not a valid input");
                   
                    
            }

        }

        public void Stop()
        {

        }
    }

    internal class PriceUpdateReceivedMessageHandler:IHandleMessages<PriceUpdateReceived>
    {
        public void Handle(PriceUpdateReceived message)
        {
            //this shows how the gateway rewrites the return address to marshal replies to and from remote sites
            Console.WriteLine("Price update received by: " + message.BranchOffice);
        }
    }
}
