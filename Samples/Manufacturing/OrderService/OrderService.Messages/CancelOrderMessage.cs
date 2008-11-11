using System;
using NServiceBus;

namespace OrderService.Messages
{
    [Serializable]
    public class CancelOrderMessage : IMessage
    {
        public CancelOrderMessage(string purchaseOrderNumber, Guid partnerId)
        {
            this.PurchaseOrderNumber = purchaseOrderNumber;
            this.PartnerId = partnerId;
        }

        public CancelOrderMessage()
        {
        }

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
