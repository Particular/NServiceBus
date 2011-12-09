using System;
using System.Linq;
using NServiceBus.Saga;
using Raven.Client;

namespace NServiceBus.SagaPersisters.Raven
{
    public class RavenSagaPersister : ISagaPersister
    {
        public IDocumentStore Store { get; set; }

        public string Database { get; set; }

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
            var luceneQuery = string.Format("{0}:{1}", property, value);

            using (var session = OpenSession())
            {
                return session.Advanced.LuceneQuery<T>()
                .Where(luceneQuery).FirstOrDefault();
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
            if (string.IsNullOrEmpty(Database))
                return Store.OpenSession();

            return Store.OpenSession(Database);
        }
    }
}