using ExternalOrderMessages;
using NServiceBus;
using NServiceBus.Saga;
using OrderQueryLogic;

namespace MessageHandlers
{
    public class OrderMessageHandler : SagaMessageHandler
    {
        public override bool NeedToHandle(IMessage message)
        {
            if (message is CreateOrderMessage || message is CancelOrderMessage)
                return true;
            else
                return base.NeedToHandle(message);
        }

        public override ISagaEntity FindSagaUsing(IMessage message)
        {
            IMessageWithOrderId order = message as IMessageWithOrderId;
            if (order != null)
                return this.query.Find(order.OrderId);
            else 
                return base.FindSagaUsing(message);
        }

        private IQuerySagasByOrderIds query;
        public IQuerySagasByOrderIds Query
        {
            set { this.query = value; }
        }
    }
}
