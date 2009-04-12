using System;
using System.Collections.Generic;
using NServiceBus;

namespace OrderService.Messages
{
    public interface OrderMessage : IMessage
    {
        bool Done { get; set; }
        DateTime ProvideBy { get; set; }
        List<OrderLine> OrderLines { get; set; }
        string PurchaseOrderNumber { get; set; }
        Guid PartnerId { get; set; }
    }
}
