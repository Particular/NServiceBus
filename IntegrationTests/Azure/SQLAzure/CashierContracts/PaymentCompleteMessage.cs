using System;
using NServiceBus;

namespace CashierContracts
{
    public class PaymentCompleteMessage : IEvent
    {
        public Guid OrderId { get; set; }

        public PaymentCompleteMessage(Guid orderId)
        {
            OrderId = orderId;
        }
    }
}
