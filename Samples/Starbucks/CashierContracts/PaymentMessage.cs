using System;
using NServiceBus;

namespace CashierContracts
{
    [Serializable]
    public class PaymentMessage : IMessage
    {
        public Decimal Amount { get; private set; }
        public Guid OrderId { get; private set; }

        public PaymentMessage(Decimal amount, Guid orderId)
        {
            Amount = amount;
            OrderId = orderId;
        }
    }
}
