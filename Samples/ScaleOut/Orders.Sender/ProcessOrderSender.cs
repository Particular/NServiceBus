using System;
using NServiceBus;
using Orders.Messages;

namespace Orders.Sender
{
    class ProcessOrderSender : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }

        public void Start()
        {
            Console.WriteLine("Press 'Enter' to send a message. To exit, Ctrl + C");
            var counter = 0;
            while (Console.ReadLine() != null)
            {
                counter++;
                var placeOrder = new PlaceOrder() { OrderId = "order" + counter};
                Bus.Send(placeOrder).Register(PlaceOrderReturnCodeHandler, this);
                Console.WriteLine(string.Format("Sent PlacedOrder command with order id [{0}].", placeOrder.OrderId));
            }
        }

        private static void PlaceOrderReturnCodeHandler(IAsyncResult asyncResult)
        {
            var result = asyncResult.AsyncState as CompletionResult;
            Console.WriteLine(string.Format("Received [{0}] Return code for Placing Order.", Enum.GetName(typeof (PlaceOrderStatus), result.ErrorCode)));
        }

        public void Stop()
        {
            
        }
    }
}
