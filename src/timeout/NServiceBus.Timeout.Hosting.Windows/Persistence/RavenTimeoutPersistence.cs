namespace NServiceBus.Timeout.Hosting.Windows.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Raven.Abstractions.Indexing;
    using Raven.Client;
    using Raven.Client.Indexes;
    using log4net;
    using Raven.Client.Linq;

    public class RavenTimeoutPersistence : IPersistTimeouts
    {
        object deleteLock = new object();
        readonly IDocumentStore store;

        public RavenTimeoutPersistence(IDocumentStore store)
        {
            this.store = store;

            store.DatabaseCommands.PutIndex("RavenTimeoutPersistence/TimeoutDataSortedByTime",
                                            new IndexDefinitionBuilder<TimeoutData>
                                                {
                                                    Map = docs => from doc in docs
                                                                  select new { doc.Time, OwningTimeoutManager = doc.OwningTimeoutManager ?? "" },
                                                    SortOptions =
                                                            {
                                                                {doc => doc.Time, SortOptions.String}
                                                            },
                                                    Indexes =
                                                            {
                                                                {doc => doc.Time, FieldIndexing.Default}
                                                            },
                                                    Stores =
                                                            {
                                                                {doc => doc.Time, FieldStorage.No}
                                                            }
                                                }, true);

            store.DatabaseCommands.PutIndex("RavenTimeoutPersistence/TimeoutData/BySagaId", new IndexDefinitionBuilder<TimeoutData>
                                                                        {
                                                                            Map = docs => from doc in docs
                                                                                          select new { doc.SagaId }
                                                                        }, true);

        }

        public List<TimeoutData> GetNextChunk(out DateTime nextTimeToRunQuery)
        {
            try
            {
                var now = DateTime.UtcNow;

                using (var session = OpenSession())
                {
                    session.Advanced.AllowNonAuthoritativeInformation = true;
                    RavenQueryStatistics stats;
                    const int maxRecords = 200;
                    var results = session.Query<TimeoutData>("RavenTimeoutPersistence/TimeoutDataSortedByTime")
                        .Where(t => (t.OwningTimeoutManager == String.Empty || t.OwningTimeoutManager == Configure.EndpointName) && t.Time <= now)
                        .OrderBy(t => t.Time)
                        .Statistics(out stats)
                        .Take(maxRecords)
                        .ToList();

                    if (stats.TotalResults > maxRecords || stats.IsStale)
                    {
                        nextTimeToRunQuery = DateTime.UtcNow;
                    }
                    else
                    {
                        nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(1);
                    }

                    return results;
                }
            }
            catch (Exception)
            {
                if ((store == null) || (string.IsNullOrWhiteSpace(store.Identifier)) || (string.IsNullOrWhiteSpace(store.Url)))
                {
                    Logger.Error("Exception occurred while trying to access Raven Database. You can check Raven availability at its console at http://localhost:8080/raven/studio.html (unless Raven defaults were changed), or make sure the Raven service is running at services.msc (services programs console).");
                    throw;
                }

                Logger.ErrorFormat(
                    "Exception occurred while trying to access Raven Database: [{0}] at [{1}]. You can check Raven availability at its console at http://localhost:8080/raven/studio.html (unless Raven defaults were changed), or make sure the Raven service is running at services.msc (services programs console).",
                    store.Identifier, store.Url);

                throw;
            }
        }

        public void Add(TimeoutData timeout)
        {
            using (var session = OpenSession())
            {
                session.Store(timeout);
                session.SaveChanges();
            }
        }

        public bool TryRemove(string timeoutId)
        {
            lock (deleteLock)
            {
                using (var session = OpenSession())
                {
                    var timeoutData = session.Load<TimeoutData>(timeoutId);

                    if (timeoutData == null)
                        return false;

                    session.Delete(timeoutData);
                    session.SaveChanges();

                    return true;
                }
            }
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            using (var session = OpenSession())
            { 
                var items = session.Query<TimeoutData>("RavenTimeoutPersistence/TimeoutData/BySagaId").Where(x => x.SagaId == sagaId);
                foreach (var item in items)
                    session.Delete(item);

                session.SaveChanges();
            }
        }

        IDocumentSession OpenSession()
        {
            var session = store.OpenSession();

            session.Advanced.AllowNonAuthoritativeInformation = false;
            session.Advanced.UseOptimisticConcurrency = true;

            return session;
        }

        static readonly ILog Logger = LogManager.GetLogger("RavenTimeoutPersistence");
    }
}