using System;
using NServiceBus;
using Orders.Messages;

namespace Orders.Sender
{
    public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
    {
        public void Handle(OrderPlaced orderPlaced)
        {
            Console.WriteLine("Received Event OrderPlaced for orderId: " + orderPlaced.OrderId);
        }
    }
    
}
