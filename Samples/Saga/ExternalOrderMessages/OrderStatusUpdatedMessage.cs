using System;
using NServiceBus;

namespace ExternalOrderMessages
{
    [Serializable]
    public class OrderStatusUpdatedMessage : IMessage
    {
        public Guid OrderId;
        public OrderStatus Status;
    }

    public enum OrderStatus
    {
        Recieved,
        Authorized1,
        Authorized2,
        Accepted,
        Cancelled,
        Rejected,
    }
}
