using System;
using System.Linq;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.Raven
{
    using global::Raven.Client;

    public class RavenSagaPersister : ISagaPersister
    {
        public IDocumentStore Store { get; set; }

        public void Save(ISagaEntity saga)
        {
            using (var session = OpenSession())
            {
                SaveSaga(session, saga);
                session.SaveChanges();
            }
        }
        
        public void Update(ISagaEntity saga)
        {
            using (var session = OpenSession())
            {
                DeleteUniquePropertyEntityIfExists(session, saga);
                StoreUniqueProperty(session, saga);
                
                //We don't actually store the saga again, since raven is tracking the entity
                //SaveSaga(session, saga);
                
                session.SaveChanges();
            }
        }

        public T Get<T>(Guid sagaId) where T : ISagaEntity
        {
            using (var session = OpenSession())
            {
                try
                {
                    return session.Load<T>(sagaId);
                }
                catch (InvalidCastException)
                {
                    return default(T);
                }
            }
        }

        public T Get<T>(string property, object value) where T : ISagaEntity
        {
            using (var session = OpenSession())
            {
                return session.Advanced.LuceneQuery<T>()
                    .WhereEquals(property, value)
                    .WaitForNonStaleResults()
                    .FirstOrDefault();
            }
        }

        public void Complete(ISagaEntity saga)
        {
            using (var session = OpenSession())
            {
                DeleteUniquePropertyEntityIfExists(session, saga);
                session.Advanced.DatabaseCommands.Delete(Store.Conventions.FindTypeTagName(saga.GetType()) + "/" + saga.Id, null);
                session.SaveChanges();
            }
        }

        IDocumentSession OpenSession()
        {
            var session = Store.OpenSession();

            session.Advanced.AllowNonAuthoritiveInformation = false;
            session.Advanced.UseOptimisticConcurrency = true;

            return session;
        }

        void SaveSaga(IDocumentSession session, ISagaEntity saga)
        {
            StoreUniqueProperty(session, saga);
            session.Store(saga);
        }
        
        static UniqueProperty GetUniqueProperty(ISagaEntity saga)
        {
            var uniqueProperty = UniqueAttribute.GetUniqueProperties(saga)
                .Select(prop => new UniqueProperty(saga, prop))
                .FirstOrDefault();

            return uniqueProperty;
        }

        private void StoreUniqueProperty(IDocumentSession session, ISagaEntity saga)
        {
            var uniqueProperty = GetUniqueProperty(saga);

            if (uniqueProperty != null)
                session.Store(uniqueProperty);
        }

        private void DeleteUniquePropertyEntityIfExists(IDocumentSession session, ISagaEntity saga)
        {
            var uniqueProperty = GetUniqueProperty(saga);

            if (uniqueProperty == null) return;

            var persistedUniqueProperty = session.Query<UniqueProperty>()
                .Customize(x => x.WaitForNonStaleResults())
                .SingleOrDefault(p => p.SagaId == saga.Id);

            if (persistedUniqueProperty != null)
                session.Delete(persistedUniqueProperty);
        }
    }
}