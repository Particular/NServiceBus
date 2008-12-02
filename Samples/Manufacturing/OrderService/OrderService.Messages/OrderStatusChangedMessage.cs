using System;
using System.Collections.Generic;
using NServiceBus;

namespace OrderService.Messages
{
    [Serializable]
    public class OrderStatusChangedMessage : IMessage
    {
        public string PurchaseOrderNumber { get; set; }
        public Guid PartnerId { get; set; }
        public OrderStatusEnum Status { get; set; }
        public List<OrderLine> OrderLines { get; set; }
    }
}
