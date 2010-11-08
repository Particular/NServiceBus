using System;
using NServiceBus;

namespace OrderService.Messages
{
    public interface CancelOrderMessage : IMessage
    {
        string PurchaseOrderNumber { get; set; }
        Guid PartnerId { get; set; }
    }
}
