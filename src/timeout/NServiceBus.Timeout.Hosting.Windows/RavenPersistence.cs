namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using global::Timeout.MessageHandlers;
    using Raven.Client.Document;
    using Raven.Client.Linq;

    public class RavenPersistence : IPersistTimeouts
    {
        private readonly DocumentStore store;

        public RavenPersistence(DocumentStore store)
        {
            this.store = store;
        }

        public IEnumerable<TimeoutData> GetAll()
        {
            using (var session = this.store.OpenSession())
                foreach (var item in session.Query<TimeoutData>())
                    yield return item;
        }
        public void Add(TimeoutData timeout)
        {
            using (var session = this.store.OpenSession())
            {
                session.Store(timeout);
                session.SaveChanges();
            }
        }
        public void Remove(Guid sagaId)
        {
            using (var session = this.store.OpenSession())
            {
                var items = session.Query<TimeoutData>().Where(x => x.SagaId == sagaId);
                foreach (var item in items)
                    session.Delete(item);

                session.SaveChanges();
            }
        }
    }
}