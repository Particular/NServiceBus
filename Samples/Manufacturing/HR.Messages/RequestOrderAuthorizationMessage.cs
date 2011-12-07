using System;
using NServiceBus.Saga;
using System.Collections.Generic;

namespace HR.Messages
{
    public interface IRequestOrderAuthorizationMessage : ISagaMessage
    {
        Guid PartnerId { get; set; }
        List<IOrderLine> OrderLines { get; set; }
    }
}
