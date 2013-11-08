using System;
using NServiceBus;
using Orders.Messages;

namespace Orders.Sender
{
    internal class ProcessOrderSender : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }

        public void Start()
        {
            Console.WriteLine("Press 'Enter' to send a message. To exit, Ctrl + C");
            int counter = 0;
            while (Console.ReadLine() != null)
            {
                counter++;
                var placeOrder = new PlaceOrder {OrderId = "order" + counter};
                Bus.Send(placeOrder).Register(PlaceOrderReturnCodeHandler, this);
                Console.WriteLine("Sent PlacedOrder command with order id [{0}].", placeOrder.OrderId);
            }
        }

        public void Stop()
        {
        }

        private static void PlaceOrderReturnCodeHandler(IAsyncResult asyncResult)
        {
            var result = asyncResult.AsyncState as CompletionResult;
            Console.WriteLine("Received [{0}] Return code for Placing Order.",
                Enum.GetName(typeof (PlaceOrderStatus), result.ErrorCode));
        }
    }
}