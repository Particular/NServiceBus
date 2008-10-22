using NServiceBus;
using OrderService.Messages;
using NServiceBus.Saga;

namespace OrderService.MessageHandlers
{
    public class OrderFinder : IFindSagas<OrderSagaData>
    {
        private IQuerySagasByPartnerIdAndPurchaseOrderNumber query;
        public IQuerySagasByPartnerIdAndPurchaseOrderNumber Query
        {
            set { this.query = value; }
        }

        public OrderSagaData FindBy(IMessage message)
        {
            IOrderSagaIdentifyingMessage identify = message as IOrderSagaIdentifyingMessage;
            if (identify == null)
                return null;

            return this.query.Query(identify.PartnerId, identify.PurchaseOrderNumber) as OrderSagaData;
        }
    }
}
