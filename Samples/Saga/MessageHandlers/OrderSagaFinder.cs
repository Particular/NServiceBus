using ExternalOrderMessages;
using NServiceBus;
using NServiceBus.Saga;
using OrderQueryLogic;
using ProcessingLogic;

namespace MessageHandlers
{
    public class OrderSagaFinder : IFindSagas<OrderSagaData>
    {
        public OrderSagaData FindBy(IMessage message)
        {
            IMessageWithOrderId order = message as IMessageWithOrderId;
            if (order == null)
                return null;

            return this.query.Find(order.OrderId) as OrderSagaData;
        }

        private IQuerySagasByOrderIds query;
        public IQuerySagasByOrderIds Query
        {
            set { this.query = value; }
        }
    }
}
