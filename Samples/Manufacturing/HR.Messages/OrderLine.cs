using System;

namespace HR.Messages
{
    [Serializable]
    public class OrderLine : IOrderLine
    {
        public OrderLine(Guid productId, float quantity)
        {
            this.productId = productId;
            this.quantity = quantity;
        }

        public OrderLine()
        {
        }

        private Guid productId;
        private float quantity;

        public Guid ProductId
        {
            get { return productId; }
            set { this.productId = value; }
        }

        public float Quantity
        {
            get { return quantity; }
            set { this.quantity = value; }
        }
    }
}
