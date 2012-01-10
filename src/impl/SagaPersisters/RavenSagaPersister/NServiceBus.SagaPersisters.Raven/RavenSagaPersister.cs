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
            StoreUniqueProperty(saga);
            Session.Store(saga);
        }

        public void Update(ISagaEntity saga)
        {
            //Don't store entity since raven tracks the entity
            DeleteUniquePropertyEntityIfExists(saga);
            StoreUniqueProperty(saga);
        }

        public T Get<T>(Guid sagaId) where T : ISagaEntity
        {
            try
            {
                return Session.Load<T>(sagaId);
            }
            catch (InvalidCastException)
            {
                return default(T);
            }
        }

        public T Get<T>(string property, object value) where T : ISagaEntity
        {
            return Session.Advanced.LuceneQuery<T>()
                .WhereEquals(property, value)
                .WaitForNonStaleResults()
                .FirstOrDefault();
        }

        public void Complete(ISagaEntity saga)
        {
                DeleteUniquePropertyEntityIfExists(saga);
                Session.Advanced.DatabaseCommands.Delete(sessionFactory.Store.Conventions.FindTypeTagName(saga.GetType()) + "/" + saga.Id, null);
        }
        
        static UniqueProperty GetUniqueProperty(ISagaEntity saga)
        {
            var uniqueProperty = UniqueAttribute.GetUniqueProperties(saga)
                .Select(prop => new UniqueProperty(saga, prop))
                .FirstOrDefault();

            return uniqueProperty;
        }

        private void StoreUniqueProperty(ISagaEntity saga)
        {
            var uniqueProperty = GetUniqueProperty(saga);
            
            if (uniqueProperty != null)
                Session.Store(uniqueProperty);
        }

        private void DeleteUniquePropertyEntityIfExists(ISagaEntity saga)
        {
            var uniqueProperty = GetUniqueProperty(saga);

            if (uniqueProperty == null) return;

            var persistedUniqueProperty = Session.Query<UniqueProperty>()
                .Customize(x => x.WaitForNonStaleResults())
                .SingleOrDefault(p => p.SagaId == saga.Id);

            if (persistedUniqueProperty != null)
                Session.Delete(persistedUniqueProperty);
        }
    }
}