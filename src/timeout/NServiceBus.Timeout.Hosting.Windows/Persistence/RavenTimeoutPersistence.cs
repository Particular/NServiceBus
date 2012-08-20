namespace NServiceBus.Timeout.Hosting.Windows.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Raven.Abstractions.Commands;
    using Raven.Abstractions.Indexing;
    using Raven.Client;
    using Raven.Client.Indexes;
    using log4net;
    using Raven.Client.Linq;

    public class RavenTimeoutPersistence : IPersistTimeouts
    {
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
                DateTime now = DateTime.UtcNow;
                using (var session = OpenSession())
                {
                    session.Advanced.AllowNonAuthoritativeInformation = false;

                    RavenQueryStatistics stats;
                    const int maxRecords = 200;
                    var results = session.Query<TimeoutData>("RavenTimeoutPersistence/TimeoutDataSortedByTime")
                        .Where(t => (t.OwningTimeoutManager == String.Empty || t.OwningTimeoutManager == Configure.EndpointName) && t.Time <= now)
                        .OrderBy(t => t.Time)
                        .Customize(c => c.WaitForNonStaleResultsAsOf(now))
                        .Statistics(out stats)
                        .Take(maxRecords)
                        .ToList();

                    if (stats.TotalResults > maxRecords)
                    {
                        nextTimeToRunQuery = DateTime.UtcNow;
                    }
                    else
                    {
                        //Retrieve next time we need to run query
                        var startOfNextChunk = session.Query<TimeoutData>("RavenTimeoutPersistence/TimeoutDataSortedByTime")
                            .Where(t => (t.OwningTimeoutManager == String.Empty || t.OwningTimeoutManager == Configure.EndpointName) && t.Time > now)
                            .OrderBy(t => t.Time)
                            .Customize(c => c.WaitForNonStaleResultsAsOf(now))
                            .FirstOrDefault();

                        if (startOfNextChunk != null)
                        {
                            nextTimeToRunQuery = startOfNextChunk.Time;
                        }
                        else //If no more documents in database then re-query in 30 minutes
                        {
                            nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(30);
                        }
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

        public void Remove(string timeoutId)
        {
            using (var session = OpenSession())
            {
                session.Advanced.Defer(new DeleteCommandData { Key = timeoutId });
                session.SaveChanges();
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

            return session;
        }

        static readonly ILog Logger = LogManager.GetLogger("RavenTimeoutPersistence");
    }
}