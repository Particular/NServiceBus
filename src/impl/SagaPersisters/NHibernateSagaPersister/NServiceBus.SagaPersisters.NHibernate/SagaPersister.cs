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
            session.Save(saga);
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
        }

        protected ISession session
        {
            get
            {
                return Thread.GetData(Thread.GetNamedDataSlot(typeof (ISession).Name)) as ISession;
            }
        }
    }
}
