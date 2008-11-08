using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace OrderService
{
    public class OrderSagaData : ISagaEntity
    {
        private Guid id;
        private string originator;

        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual string Originator
        {
            get { return originator; }
            set { originator = value; }
        }

        public virtual string PurchaseOrderNumber
        {
            get { return purchaseOrderNumber; }
            set { purchaseOrderNumber = value; }
        }

        public virtual Guid PartnerId
        {
            get { return partnerId; }
            set { partnerId = value; }
        }

        public virtual DateTime ProvideBy
        {
            get { return provideBy; }
            set { provideBy = value; }
        }

        public virtual void UpdateOrderLine(Guid productId, float quantity)
        {
            bool found = false;

            foreach (OrderLine line in orderLines)
                if (line.ProductId == productId)
                {
                    line.Quantity = quantity;
                    found = true;
                }

            if (!found)
                orderLines.Add(new OrderLine(this, productId, quantity));
        }

        public virtual void UpdateAuthorization(bool authorized, Guid productId, float quantity)
        {
            OrderLine toRemove = null;

            foreach (OrderLine line in orderLines)
                if (line.ProductId == productId)
                    if (authorized)
                        line.AuthorizedQuantity = quantity;
                    else
                        toRemove = line;

            if (toRemove != null)
                orderLines.Remove(toRemove);
        }

        public virtual bool IsAuthorized
        {
            get
            {
                foreach(OrderLine line in orderLines)
                    if (line.Quantity != line.AuthorizedQuantity)
                        return false;

                return true;
            }
        }

        public virtual IEnumerable<OrderLine> Lines
        {
            get { return orderLines; }
        }

        private string purchaseOrderNumber;
        private Guid partnerId;
        private DateTime provideBy;

        private IList<OrderLine> orderLines = new List<OrderLine>();
    }

    public class OrderLine
    {
        public OrderLine() { }
        public OrderLine(OrderSagaData parent, Guid productId, float quantity)
        {
            this.order = parent;
            this.productId = productId;
            this.quantity = quantity;
        }

        private Guid id;

        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        private OrderSagaData order;

        public virtual OrderSagaData Order
        {
            get { return order; }
            set { order = value; }
        }

        private Guid productId;

        public virtual Guid ProductId
        {
            get { return productId; }
            set { productId = value; }
        }

        private float quantity;

        public virtual float Quantity
        {
            get { return quantity; }
            set { quantity = value; }
        }

        public virtual float AuthorizedQuantity
        {
            get { return authorizedQuantity; }
            set { authorizedQuantity = value; }
        }

        private float authorizedQuantity;
    }
}
