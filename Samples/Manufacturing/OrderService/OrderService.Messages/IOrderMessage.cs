using System;
using System.Collections.Generic;
using NServiceBus;

namespace OrderService.Messages
{
    public interface IOrderMessage : IMessage
    {
        bool Done { get; set; }
        DateTime ProvideBy { get; set; }
        List<IOrderLine> OrderLines { get; set; }
        string PurchaseOrderNumber { get; set; }
        Guid PartnerId { get; set; }
    }
}
