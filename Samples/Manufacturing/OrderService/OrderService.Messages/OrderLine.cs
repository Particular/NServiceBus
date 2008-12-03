using System;
using HR.Messages;
using NServiceBus;

namespace OrderService.Messages
{
    public interface OrderLine : IMessage
    {
        Guid ProductId { get; set; }
        float Quantity { get; set; }
    }
}
