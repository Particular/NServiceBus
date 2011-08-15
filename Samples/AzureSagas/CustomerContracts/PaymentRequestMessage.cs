using System;
using NServiceBus;

namespace CustomerContracts
{
    public class PaymentRequestMessage : IMessage
    {
        public Double Amount { get; set; }
        public String CustomerName { get; set; }
        public Guid OrderId { get; set; }

        public PaymentRequestMessage()
        {
        }


        public PaymentRequestMessage(Double amount, String customerName, Guid orderId)
        {
            Amount = amount;
            CustomerName = customerName;
            OrderId = orderId;
        }
    }
}
