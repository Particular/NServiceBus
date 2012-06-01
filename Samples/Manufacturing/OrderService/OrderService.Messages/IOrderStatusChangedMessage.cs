using System;
using System.Collections.Generic;
using NServiceBus;

namespace OrderService.Messages
{
    public interface IOrderStatusChangedMessage : IMessage
    {
        string PurchaseOrderNumber { get; set; }
        Guid PartnerId { get; set; }
        OrderStatusEnum Status { get; set; }
        List<IOrderLine> OrderLines { get; set; }
    }
}
