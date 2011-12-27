using System;
using NServiceBus;

namespace CashierContracts
{
    [Serializable]
    public class OrderPaid : IMessage
    {
        public Guid OrderId { get; private set; }

        public OrderPaid(Guid orderId)
        {
            OrderId = orderId;
        }
    }
}
