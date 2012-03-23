namespace NServiceBus.Timeout.Hosting.Windows.Persistence
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Raven.Client;
    using Raven.Client.Linq;
    using Raven.Abstractions.Commands;
    using Raven.Client.Shard;

    public class RavenTimeoutPersistence : IPersistTimeouts
    {
        private readonly IDocumentStore store;

        public RavenTimeoutPersistence(IDocumentStore store)
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

        public void RemoveTimeout(Guid timeoutId)
        {
            using (var session = OpenSession())
            {
                try
                {
                    session.Advanced.Defer(new DeleteCommandData { Key = "TimeoutData/" + timeoutId });
                }
                catch (NotSupportedException)
                {
                    // 2012.03.23: Advanced.Defer is not yet supported by ShardedDocumentSession,
                    // but will be in next version according to Oren Eini.
                    var entity = session.Load<TimeoutData>("TimeoutData/" + timeoutId);
                    session.Delete(entity);
                }

                session.SaveChanges();
            }
        }

        private IDocumentSession OpenSession()
        {
            return store.OpenSession();
        }
    }
}