

using System;
using System.Collections.Generic;
using NServiceBus;

namespace ExternalOrderMessages
{
    [Serializable]
    public class CreateOrderMessage : IMessageWithOrderId
    {
        public List<Guid> Products;
        public List<float> Amounts;
        public Guid CustomerId;
        public bool Completed;
        public DateTime ProvideBy;

        private Guid orderId;

        public Guid OrderId
        {
            get { return orderId; }
            set { orderId = value; }
        }
    }

    public interface IMessageWithOrderId : IMessage
    {
        Guid OrderId { get; }
    }
}
