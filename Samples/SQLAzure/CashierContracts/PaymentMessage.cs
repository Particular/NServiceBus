using System;
using NServiceBus;

namespace CashierContracts
{
    public class PaymentMessage : IMessage
    {
        public Double Amount { get; set; }
        public Guid OrderId { get; set; }

        public PaymentMessage(Double amount, Guid orderId)
        {
            Amount = amount;
            OrderId = orderId;
        }
    }
}
