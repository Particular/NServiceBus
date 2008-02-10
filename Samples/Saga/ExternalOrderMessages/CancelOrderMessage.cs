using System;

namespace ExternalOrderMessages
{
    [Serializable]
    public class CancelOrderMessage : IMessageWithOrderId
    {
        private Guid orderId;
        public Guid CustomerId;

        public Guid OrderId
        {
            get { return orderId; }
            set { orderId = value; }
        }
    }
}
