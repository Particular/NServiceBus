namespace Headquarter.Messages
{
    using System;
    using NServiceBus;

    public class PriceUpdated : IMessage
    {
        public int ProductId { get; set; }
        public double NewPrice { get; set; }
        public DateTime ValidFrom { get; set; }

        public DataBusProperty<string> SomeLargeString { get; set; }
    }
}
