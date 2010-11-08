using System;
using NServiceBus;

namespace CustomerContracts
{
    [Serializable]
    public class PaymentRequestMessage : IMessage
    {
        public Double Amount { get; private set; }
        public String CustomerName { get; private set; }
        public Guid OrderId { get; private set; }

        public PaymentRequestMessage(Double amount, String customerName, Guid orderId)
        {
            Amount = amount;
            CustomerName = customerName;
            OrderId = orderId;
        }
    }
}
