using System;
using System.Linq;
using NServiceBus.Persistence.Raven;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.Raven
{
    using global::Raven.Client;

    public class RavenSagaPersister : ISagaPersister
    {
        readonly RavenSessionFactory sessionFactory;

        protected IDocumentSession Session { get { return sessionFactory.Session; } }

        public RavenSagaPersister(RavenSessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public void Save(ISagaEntity saga)
        {
            Session.Store(saga);
        }

        public void Update(ISagaEntity saga)
        {
            //Do not re-save saga entity, since raven is tracking the entity
        }

        public T Get<T>(Guid sagaId) where T : ISagaEntity
        {
            return Get<T>("Id", sagaId);
        }

        public T Get<T>(string property, object value) where T : ISagaEntity
        {
            try
            {
                return Session.Advanced.LuceneQuery<T>()
                    .WhereEquals(property, value)
                    .WaitForNonStaleResults()
                    .FirstOrDefault();
            }
            catch (InvalidCastException)
            {
                return default(T);
            }
        }

        public void Complete(ISagaEntity saga)
        {
            Session.Delete(saga);
        }
    }
}