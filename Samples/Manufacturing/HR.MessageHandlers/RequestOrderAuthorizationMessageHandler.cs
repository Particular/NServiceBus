using System;
using HR.Messages;
using NServiceBus;
using System.Threading;

namespace HR.MessageHandlers
{
    public class RequestOrderAuthorizationMessageHandler : IHandleMessages<RequestOrderAuthorizationMessage>
    {
        public void Handle(RequestOrderAuthorizationMessage message)
        {
            Console.WriteLine("Recieved Message: " + message);
            Console.WriteLine("     PartnerId: " + message.PartnerId);
            foreach (var orderLine in message.OrderLines)
                Console.WriteLine("     orderLine productId: {0}, Quantity {1}", orderLine.ProductId, orderLine.Quantity);    
            
            
            if (message.OrderLines != null)
                foreach(IOrderLine ol in message.OrderLines)
                    if (ol.Quantity > 50F)
                        Thread.Sleep(10000);

            // This will reply back to the order saga instance that sent us this message. NServiceBus is
            // automatically doing the correlationF
            Bus.Reply<OrderAuthorizationResponseMessage>(m =>
                                                             {
                                                                 m.Success = true; 
                                                                 m.OrderLines = message.OrderLines;
                                                             });
        }

        public IBus Bus { get; set; }
    }
}
