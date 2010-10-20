using System;
using NServiceBus;

namespace CashierContracts
{
    [Serializable]
    public class PaymentMessage : IMessage
    {
        public Double Amount { get; private set; }
        public Guid OrderId { get; private set; }

        public PaymentMessage(Double amount, Guid orderId)
        {
            Amount = amount;
            OrderId = orderId;
        }
    }
}
