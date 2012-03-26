namespace NServiceBus.Timeout.Hosting.Windows.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Raven.Abstractions.Commands;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Linq;

    public class RavenTimeoutPersistence : IPersistTimeouts
    {
        readonly IDocumentStore store;

        public RavenTimeoutPersistence(IDocumentStore store)
        {
            this.store = store;
        }

        public IEnumerable<TimeoutData> GetAll()
        {
            using (var session = OpenSession())
                return session.Query<TimeoutData>().ToList();
        }
        public void Add(TimeoutData timeout)
        {
            using (var session = OpenSession())
            {
                session.Store(timeout);
                session.SaveChanges();
            }
        }

        public void Remove(TimeoutData timeout)
        {
            using (var session = OpenSession())
            {
                session.Advanced.Defer(new DeleteCommandData { Key = timeout.Id });
                session.SaveChanges();
            }

        }

        public void ClearTimeoutsFor(Guid sagaId)
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
            var session = store.OpenSession();

            session.Advanced.AllowNonAuthoritativeInformation = false;

            return session;
        }
    }
}