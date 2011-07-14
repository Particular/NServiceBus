using System;
using System.Collections.Generic;
using NServiceBus;

namespace OrderService.Messages
{
    public interface OrderStatusChangedMessage : IMessage
    {
        string PurchaseOrderNumber { get; set; }
        Guid PartnerId { get; set; }
        OrderStatusEnum Status { get; set; }
        List<OrderLine> OrderLines { get; set; }
    }
}
