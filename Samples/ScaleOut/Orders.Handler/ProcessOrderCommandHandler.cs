using System.Threading;
using NServiceBus;
using Orders.Messages;
using log4net;

namespace Orders.Handler
{
    public class ProcessOrderCommandHandler : IHandleMessages<PlaceOrder>
    {
        public IBus Bus { get; set; }
        public void Handle(PlaceOrder placeOrder)
        {
            Logger.Info("Received ProcessOrder command, order Id: " + placeOrder.OrderId);
            Bus.Return(PlaceOrderStatus.Ok);
            Logger.InfoFormat("Sent Ok status for orderId [{0}].", placeOrder.OrderId);

            // Process Order...
            Logger.Info("Processing received order....");
            Thread.Sleep(3000);
            
            Bus.Publish<OrderPlaced>(m => m.OrderId = placeOrder.OrderId);
            Logger.InfoFormat("Sent Order placed event for orderId [{0}].", placeOrder.OrderId);
        }
        public static readonly ILog Logger = LogManager.GetLogger("ProcessOrderCommandHandler");
    }
}
