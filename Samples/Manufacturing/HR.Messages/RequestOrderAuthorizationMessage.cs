using System;
using System.Collections.Generic;

namespace HR.Messages
{
    using NServiceBus;

    public class RequestOrderAuthorizationMessage : IMessage
    {
        public Guid PartnerId { get; set; }
        public List<IOrderLine> OrderLines { get; set; }
    }
}
