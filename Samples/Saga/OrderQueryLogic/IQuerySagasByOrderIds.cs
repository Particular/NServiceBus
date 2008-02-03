using System;
using NServiceBus.Saga;
using NServiceBus;

namespace OrderQueryLogic
{
    public interface IQuerySagasByOrderIds
    {
        ISaga<T> Find<T>(Guid orderId) where T : IMessage;
    }
}
