namespace Headquarter
{
    using System;
    using Messages;
    using NServiceBus;

    public class UpdatePriceMessageHandler:IHandleMessages<UpdatePrice>
    {
        public IBus Bus { get; set; }

        public void Handle(UpdatePrice message)
        {
            Console.WriteLine("Price update request received from the webclient, going to push it to the remote sites");

            Bus.SendToSites(new[] { "SiteA", "SiteB" }, new PriceUpdated
            {
                ProductId = message.ProductId,
                NewPrice = message.NewPrice,
                ValidFrom = message.ValidFrom,
                SomeLargeString = new DataBusProperty<string>("A large string to demonstrate that the gateway supports databus properties")
            });
        }
    }
}