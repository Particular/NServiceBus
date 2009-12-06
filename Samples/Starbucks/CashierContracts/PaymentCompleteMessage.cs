using System;
using NServiceBus;

namespace CashierContracts
{
    [Serializable]
    public class PaymentCompleteMessage : IMessage
    {
        public Guid OrderId { get; private set; }

        public PaymentCompleteMessage(Guid orderId)
        {
            OrderId = orderId;
        }
    }
}
