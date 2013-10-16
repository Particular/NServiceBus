namespace SiteA
{
    using System;
    using Headquarter.Messages;
    using NServiceBus;

    public class PriceUpdatedHandler : IHandleMessages<PriceUpdated>
    {
        public IBus Bus { get; set; }

        public void Handle(PriceUpdated message)
        {
            Console.WriteLine("Price update for product: " + message.ProductId +  " received. Going to reply over channel: " + message.GetHeader(Headers.OriginatingSite));


            //this shows how the gateway rewrites the return address to marshal replies to and from remote sites
            Bus.Reply<PriceUpdateReceived>(m=>
                {
                    m.BranchOffice = "SiteA";
                });
        }
    }
}
