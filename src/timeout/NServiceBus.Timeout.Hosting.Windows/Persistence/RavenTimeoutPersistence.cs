namespace NServiceBus.Timeout.Hosting.Windows.Persistence
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Linq;

    public class RavenTimeoutPersistence : IPersistTimeouts
    {
        private readonly DocumentStore store;

        public RavenTimeoutPersistence(DocumentStore store)
        {
            this.store = store;
        }

        public IEnumerable<TimeoutData> GetAll()
        {
            using (var session = OpenSession())
                foreach (var item in session.Query<TimeoutData>())
                    yield return item;
        }
        public void Add(TimeoutData timeout)
        {
            using (var session = OpenSession())
            {
                session.Store(timeout);
                session.SaveChanges();
            }
        }
        public void Remove(Guid sagaId)
        {
            using (var session = OpenSession())
            {
                var items = session.Query<TimeoutData>().Where(x => x.SagaId == sagaId);
                foreach (var item in items)
                    session.Delete(item);

                session.SaveChanges();
            }
        }

        IDocumentSession OpenSession()
        {
            if (string.IsNullOrEmpty(Database))
                return store.OpenSession();

            return store.OpenSession(Database);
        }

        public string Database { get; set; }
    }
}