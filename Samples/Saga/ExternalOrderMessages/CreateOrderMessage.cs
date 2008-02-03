

using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Saga;

namespace ExternalOrderMessages
{
    [Serializable]
    public class CreateOrderMessage : IMessage
    {
        public List<Guid> Products;
        public List<float> Amounts;
        public Guid CustomerId;
        public Guid OrderId;
        public bool Completed;
        public DateTime ProvideBy;
    }
}
