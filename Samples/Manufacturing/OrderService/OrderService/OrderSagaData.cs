using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace OrderService
{
    public class OrderSagaData : ISagaEntity
    {
        public virtual Guid Id { get; set; }
        public virtual string Originator { get; set; }
        public virtual string OriginalMessageId { get; set; }
        public virtual string PurchaseOrderNumber { get; set; }
        public virtual Guid PartnerId { get; set; }
        public virtual DateTime ProvideBy { get; set; }

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
                orderLines.Add(new OrderLine { Order = this, ProductId = productId, Quantity = quantity });
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

        private IList<OrderLine> orderLines = new List<OrderLine>();
    }

    public class OrderLine
    {
        public virtual Guid Id { get; set; }
        public virtual OrderSagaData Order { get; set; }
        public virtual Guid ProductId { get; set; }
        public virtual float Quantity { get; set; }
        public virtual float AuthorizedQuantity { get; set; }
    }
}
