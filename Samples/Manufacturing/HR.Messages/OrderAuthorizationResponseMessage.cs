using System.Collections.Generic;

namespace HR.Messages
{
    using NServiceBus;

    public class OrderAuthorizationResponseMessage:IMessage
    {
        public bool Success { get; set; }
        public List<IOrderLine> OrderLines { get; set; }
    }
}
