using System;
using NServiceBus.Saga;
using OrderService.Messages;
using NHibernate;
using NHibernate.Criterion;

namespace OrderService.Persistence
{
    public class OrderSagaFinder : 
        IFindSagas<OrderSagaData>.Using<OrderMessage>,
        IFindSagas<OrderSagaData>.Using<CancelOrderMessage>
    {
        public OrderSagaData FindBy(OrderMessage message)
        {
            return FindBy(message.PurchaseOrderNumber, message.PartnerId);
        }

        public OrderSagaData FindBy(CancelOrderMessage message)
        {
            return FindBy(message.PurchaseOrderNumber, message.PartnerId);
        }

        public OrderSagaData FindBy(string purchaseOrderNumber, Guid partnerId)
        {
            return SessionFactory.GetCurrentSession().CreateCriteria(typeof(OrderSagaData))
                .Add(Expression.Eq("PurchaseOrderNumber", purchaseOrderNumber))
                .Add(Expression.Eq("PartnerId", partnerId))
                .UniqueResult<OrderSagaData>();
        }

        public ISessionFactory SessionFactory { get; set; }
    }
}
