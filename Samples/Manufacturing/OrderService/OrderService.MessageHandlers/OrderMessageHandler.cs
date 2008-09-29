using NServiceBus;
using OrderService.Messages;
using NServiceBus.Saga;

namespace OrderService.MessageHandlers
{
    public class OrderMessageHandler : SagaMessageHandler
    {
        public override ISagaEntity FindSagaUsing(IMessage message)
        {
            IOrderSagaIdentifyingMessage identify = message as IOrderSagaIdentifyingMessage;
            if (identify == null)
                return base.FindSagaUsing(message);

            return this.query.Query(identify.PartnerId, identify.PurchaseOrderNumber);
        }

        public override bool NeedToHandle(IMessage message)
        {
            if (message is OrderMessage || message is CancelOrderMessage)
                return true;

            return base.NeedToHandle(message);
        }

        private IQuerySagasByPartnerIdAndPurchaseOrderNumber query;
        public IQuerySagasByPartnerIdAndPurchaseOrderNumber Query
        {
            set { this.query = value; }
        }
    }
}
