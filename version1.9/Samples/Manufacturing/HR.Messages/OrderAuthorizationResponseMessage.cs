using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace HR.Messages
{
    public interface OrderAuthorizationResponseMessage : ISagaMessage
    {
        bool Success { get; set; }
        List<IOrderLine> OrderLines { get; set; }
    }
}
