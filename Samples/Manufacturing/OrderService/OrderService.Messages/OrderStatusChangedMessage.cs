using System;
using System.Collections.Generic;
using NServiceBus;

namespace OrderService.Messages
{
    [Serializable]
    public class OrderStatusChangedMessage : IMessage
    {
        public OrderStatusChangedMessage(string purchaseOrderNumber, Guid partnerId, OrderStatusEnum status, List<OrderLine> orderLines)
        {
            this.PurchaseOrderNumber = purchaseOrderNumber;
            this.PartnerId = partnerId;
            this.Status = status;
            this.OrderLines = (orderLines ?? new List<OrderLine>());
        }

        public OrderStatusChangedMessage()
        {
        }

        public string PurchaseOrderNumber;
        public Guid PartnerId;
        public OrderStatusEnum Status;
        public List<OrderLine> OrderLines;
    }
}
