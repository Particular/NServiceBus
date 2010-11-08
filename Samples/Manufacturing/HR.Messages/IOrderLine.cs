using System;
using NServiceBus;

namespace HR.Messages
{
    public interface IOrderLine : IMessage
    {
        Guid ProductId { get; set; }
        float Quantity { get; set; }
    }
}
