using System;
using NServiceBus;

namespace OrderService.Messages
{
    public interface IOrderSagaIdentifyingMessage : IMessage
    {
        string PurchaseOrderNumber { get; }
        Guid PartnerId { get; }
    }
}
