using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace OrderService
{
    public class OrderSagaData : IContainSagaData
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

            foreach (OrderLine line in OrderLines)
                if (line.ProductId == productId)
                {
                    line.Quantity = quantity;
                    found = true;
                }

            if (!found)
                OrderLines.Add(new OrderLine { Order = this, ProductId = productId, Quantity = quantity });
        }

        public virtual void UpdateAuthorization(bool authorized, Guid productId, float quantity)
        {
            OrderLine toRemove = null;

            foreach (OrderLine line in OrderLines)
                if (line.ProductId == productId)
                    if (authorized)
                        line.AuthorizedQuantity = quantity;
                    else
                        toRemove = line;

            if (toRemove != null)
                OrderLines.Remove(toRemove);
        }

        public virtual bool IsAuthorized
        {
            get
            {
                foreach(OrderLine line in OrderLines)
                    if (line.Quantity != line.AuthorizedQuantity)
                        return false;

                return true;
            }
        }


        private IList<OrderLine> orderLines;

        public virtual IList<OrderLine> OrderLines
        {
            get
            {
                if (orderLines == null)
                    orderLines = new List<OrderLine>();
                return orderLines;
            }
            set { orderLines = value; }
        }
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
