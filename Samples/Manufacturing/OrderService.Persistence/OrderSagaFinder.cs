using NServiceBus;
using NServiceBus.Saga;
using OrderService.Messages;
using NHibernate;
using NHibernate.Criterion;

namespace OrderService.Persistence
{
    public class OrderSagaFinder : IFindSagas<OrderSagaData>
    {
        public OrderSagaData FindBy(IMessage message)
        {
            IOrderSagaIdentifyingMessage specific = message as IOrderSagaIdentifyingMessage;
            if (specific == null)
                return null;

            return sessionFactory.GetCurrentSession().CreateCriteria(typeof (OrderSagaData))
                .Add(Expression.Eq("PurchaseOrderNumber", specific.PurchaseOrderNumber))
                .Add(Expression.Eq("PartnerId", specific.PartnerId))
                .UniqueResult<OrderSagaData>();
        }

        private ISessionFactory sessionFactory;

        public virtual ISessionFactory SessionFactory
        {
            get { return sessionFactory; }
            set { sessionFactory = value; }
        }
    }
}
