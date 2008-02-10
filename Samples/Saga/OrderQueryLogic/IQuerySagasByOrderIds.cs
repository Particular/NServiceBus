using System;
using NServiceBus.Saga;

namespace OrderQueryLogic
{
    public interface IQuerySagasByOrderIds
    {
        ISagaEntity Find(Guid orderId);
    }
}
