using System;
using NServiceBus.Saga;
using NHibernate;

namespace NServiceBus.SagaPersisters.NHibernate
{
    public class SagaPersister : ISagaPersister
    {
        public void Save(ISagaEntity saga)
        {
            session.SaveOrUpdate(saga);
        }

        public void Update(ISagaEntity saga)
        {
            session.Update(saga);
        }

        public ISagaEntity Get(Guid sagaId)
        {
            return session.Get<ISagaEntity>(sagaId);
        }

        public void Complete(ISagaEntity saga)
        {
            session.Delete(saga);
        }

        public void Dispose()
        {
            session.Close();
        }

        private ISession _session;
        protected ISession session
        {
            get
            {
                if (_session == null)
                    _session = sessionFactory.OpenSession();

                return _session;
            }
        }
        public static ISessionFactory sessionFactory;

    }
}
