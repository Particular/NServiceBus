using System;
using NServiceBus;

namespace CustomerContracts
{
    [Serializable]
    public class PaymentRequestMessage : IMessage
    {
        public Decimal Amount { get; private set; }
        public String CustomerName { get; private set; }
        public Guid OrderId { get; private set; }

        public PaymentRequestMessage(Decimal amount, String customerName, Guid orderId)
        {
            Amount = amount;
            CustomerName = customerName;
            OrderId = orderId;
        }
    }
}
