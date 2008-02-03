using System;
using NServiceBus;

namespace ExternalOrderMessages
{
    [Serializable]
    public class CancelOrderMessage : IMessage
    {
        public Guid OrderId;
        public Guid CustomerId;
    }
}
