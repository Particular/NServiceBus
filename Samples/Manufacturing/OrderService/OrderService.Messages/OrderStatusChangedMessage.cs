using System;
using System.Collections.Generic;

namespace OrderService.Messages
{
    public class OrderStatusChangedMessage : IOrderStatusChangedMessage
    {
        public string PurchaseOrderNumber { get; set; }
        public Guid PartnerId { get; set; }
        public OrderStatusEnum Status { get; set; }
        public List<IOrderLine> OrderLines { get; set; }
    }
}