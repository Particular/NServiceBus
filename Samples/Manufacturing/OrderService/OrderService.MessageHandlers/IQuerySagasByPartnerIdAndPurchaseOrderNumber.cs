using System;
using NServiceBus.Saga;

namespace OrderService.MessageHandlers
{
    public interface IQuerySagasByPartnerIdAndPurchaseOrderNumber
    {
        ISagaEntity Query(Guid partnerId, string purchaseOrderNumber);
    }
}
