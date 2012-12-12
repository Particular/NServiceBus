using NServiceBus;
using Orders.Messages;

namespace Orders.Handler
{
    using System;

    public class ProcessOrderCommandHandler : IHandleMessages<PlaceOrder>
    {
        public IBus Bus { get; set; }
        public void Handle(PlaceOrder placeOrder)
        {
            Console.Out.WriteLine("Received ProcessOrder command, order Id: " + placeOrder.OrderId);
            Bus.Return(PlaceOrderStatus.Ok);
            Console.Out.WriteLine("Sent Ok status for orderId [{0}].", placeOrder.OrderId);

            // Process Order...
            Console.Out.WriteLine("Processing received order....");
            
            Bus.Publish<OrderPlaced>(m => m.OrderId = placeOrder.OrderId);
            Console.Out.WriteLine("Sent Order placed event for orderId [{0}].", placeOrder.OrderId);
        }
    }
}
