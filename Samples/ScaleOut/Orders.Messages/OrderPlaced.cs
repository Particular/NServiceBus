using System;
using System.Text;
using NServiceBus;

namespace Orders.Messages
{
    public class OrderPlaced : IEvent
    {
        public string OrderId { get; set; }
    }
}
