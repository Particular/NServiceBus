using System;
using System.Collections.Generic;

namespace OrderService.Messages
{
    public class OrderMessage : IOrderMessage
    {
        public bool Done { get; set; }
        public DateTime ProvideBy { get; set; }
        public List<IOrderLine> OrderLines { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public Guid PartnerId { get; set; }
    }
}