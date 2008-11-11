using System;
using System.Collections.Generic;
using NServiceBus;

namespace OrderService.Messages
{
    [Serializable]
    public class OrderMessage : IMessage
    {
        public OrderMessage(string purchaseOrderNumber, Guid partnerId, bool done, DateTime provideBy, List<OrderLine> orderLines)
        {
            this.PurchaseOrderNumber = purchaseOrderNumber;
            this.PartnerId = partnerId;
            this.Done = done;
            this.ProvideBy = provideBy;
            this.OrderLines = (orderLines ?? new List<OrderLine>());
        }

        public OrderMessage()
        {
        }

        public bool Done;
        public DateTime ProvideBy;
        public List<OrderLine> OrderLines;
        private string purchaseOrderNumber;
        private Guid partnerId;

        public string PurchaseOrderNumber
        {
            get { return purchaseOrderNumber; }
            set { purchaseOrderNumber = value; }
        }

        public Guid PartnerId
        {
            get { return partnerId; }
            set { partnerId = value; }
        }
    }
}
