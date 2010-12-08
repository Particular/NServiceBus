using System;
using NServiceBus;
using OrderService.Messages;

namespace Partner
{
    public class OrderStatusChangedMessageHandler : IHandleMessages<OrderStatusChangedMessage>
    {
        public void Handle(OrderStatusChangedMessage message)
        {
            Console.WriteLine("Received status {0} for PO Number {1}.", Enum.GetName(typeof(OrderStatusEnum), message.Status), message.PurchaseOrderNumber);
        }
    }

}
