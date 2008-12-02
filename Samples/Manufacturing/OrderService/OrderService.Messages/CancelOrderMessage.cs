using System;
using NServiceBus;

namespace OrderService.Messages
{
    [Serializable]
    public class CancelOrderMessage : IMessage
    {
        public string PurchaseOrderNumber { get; set; }
        public Guid PartnerId { get; set; }
    }
}
