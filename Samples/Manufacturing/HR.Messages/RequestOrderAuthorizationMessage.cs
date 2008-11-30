using System;
using NServiceBus.Saga;
using System.Collections.Generic;

namespace HR.Messages
{
    public interface RequestOrderAuthorizationMessage : ISagaMessage
    {
        Guid PartnerId { get; set; }
        IList<IOrderLine> OrderLines { get; set; }
    }
}
