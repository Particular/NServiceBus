using System;
using NServiceBus.Saga;
using NHibernate;
using System.Threading;

namespace NServiceBus.SagaPersisters.NHibernate
{
    public class SagaPersister : ISagaPersister
    {
        public void Save(ISagaEntity saga)
        {
            sessionFactory.GetCurrentSession().Save(saga);
        }

        public void Update(ISagaEntity saga)
        {
            sessionFactory.GetCurrentSession().Update(saga);
        }

        public ISagaEntity Get(Guid sagaId)
        {
            return sessionFactory.GetCurrentSession().Get<ISagaEntity>(sagaId);
        }

        public void Complete(ISagaEntity saga)
        {
            sessionFactory.GetCurrentSession().Delete(saga);
        }

        public void Dispose()
        {
        }

        private ISessionFactory sessionFactory;

        public virtual ISessionFactory SessionFactory
        {
            get { return sessionFactory; }
            set { sessionFactory = value; }
        }
    }
}
