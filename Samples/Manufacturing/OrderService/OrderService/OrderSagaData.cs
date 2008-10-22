using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace OrderService
{
    public class OrderSagaData : ISagaEntity
    {
        private Guid id;
        private string originator;

        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Originator
        {
            get { return originator; }
            set { originator = value; }
        }

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

        public DateTime ProvideBy
        {
            get { return provideBy; }
            set { provideBy = value; }
        }

        public Dictionary<Guid, float> OrderData
        {
            get { return orderData; }
            set { orderData = value; }
        }

        public Dictionary<Guid, float> AuthorizedOrderData
        {
            get { return authorizedOrderData; }
            set { authorizedOrderData = value; }
        }

        private string purchaseOrderNumber;
        private Guid partnerId;
        private DateTime provideBy;

        private Dictionary<Guid, float> orderData = new Dictionary<Guid, float>();
        private Dictionary<Guid, float> authorizedOrderData = new Dictionary<Guid, float>();
    }
}
