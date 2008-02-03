using System;
using System.Collections.Generic;
using NServiceBus;

namespace ExternalOrderMessages
{
    [Serializable]
    public class OrderAcceptedMessage : IMessage
    {
        public List<Guid> Products;
        public List<float> Amounts;
        public Guid CustomerId;
    }
}
