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
                session.Store(saga);
                session.SaveChanges();
            }
        }

        public void Update(ISagaEntity saga)
        {
            using (var session = OpenSession())
            {
                session.Store(saga);
                session.SaveChanges();
            }
        }

        public T Get<T>(Guid sagaId) where T : ISagaEntity
        {
            using (var session = OpenSession())
            {
                return session.Load<T>(sagaId);
            }
        }

        public T Get<T>(string property, object value) where T : ISagaEntity
        {
            using (var session = OpenSession())
            {
                return session.Advanced.LuceneQuery<T>()
                .WhereEquals(property, value).FirstOrDefault();
            }
        }

        public void Complete(ISagaEntity saga)
        {
            using (var session = OpenSession())
            {
                session.Advanced.DatabaseCommands.Delete(Store.Conventions.FindTypeTagName(saga.GetType()) + "/" + saga.Id, null);
                session.SaveChanges();             
            }
        }

        IDocumentSession OpenSession()
        {
            return Store.OpenSession();
        }
    }
}