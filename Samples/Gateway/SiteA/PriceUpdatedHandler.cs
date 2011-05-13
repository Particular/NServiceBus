using NServiceBus;

namespace SiteA
{
    using System;
    using Headquarter.Messages;

    public class PriceUpdatedHandler : IHandleMessages<PriceUpdated>
    {
        public IBus Bus { get; set; }

        public void Handle(PriceUpdated message)
        {
            Console.WriteLine("Price update for product: " + message.ProductId);
        }
    }
}
