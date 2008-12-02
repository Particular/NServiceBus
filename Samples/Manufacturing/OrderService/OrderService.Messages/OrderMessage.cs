using System;
using System.Collections.Generic;
using NServiceBus;

namespace OrderService.Messages
{
    [Serializable]
    public class OrderMessage : IMessage
    {
        public bool Done { get; set; }
        public DateTime ProvideBy { get; set; }
        public List<OrderLine> OrderLines { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public Guid PartnerId { get; set; }
    }
}
