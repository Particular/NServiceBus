namespace NServiceBus.Timeout.Hosting.Windows.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Logging;
    using Raven.Abstractions.Indexing;
    using Raven.Client;
    using Raven.Client.Indexes;
    using Raven.Client.Linq;

    public class RavenTimeoutPersistence : IPersistTimeouts
    {
        readonly IDocumentStore store;

        public RavenTimeoutPersistence(IDocumentStore store)
        {
            this.store = store;

            store.DatabaseCommands.PutIndex("RavenTimeoutPersistence/TimeoutData/BySagaId", new IndexDefinitionBuilder<TimeoutData>
            {
                Map = docs => from doc in docs
                              select new { doc.SagaId }
            }, true);

            store.DatabaseCommands.PutIndex("RavenTimeoutPersistence/TimeoutDataSortedByTime",
                                            new IndexDefinitionBuilder<TimeoutData>
                                                {
                                                    Map = docs => from doc in docs
                                                                  select new { doc.Time, OwningTimeoutManager = doc.OwningTimeoutManager ?? String.Empty },
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

        }

        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            try
            {
                var now = DateTime.UtcNow;
                var skip = 0;
                var results = new List<Tuple<string, DateTime>>();
                var numberOfRequestsExecutedSoFar = 0;
                RavenQueryStatistics stats;

                do
                {
                    using (var session = OpenSession())
                    {
                        session.Advanced.AllowNonAuthoritativeInformation = true;

                        var query = session.Query<TimeoutData>("RavenTimeoutPersistence/TimeoutDataSortedByTime")
                            .Where(
                                t =>
                                    t.OwningTimeoutManager == String.Empty ||
                                    t.OwningTimeoutManager == Configure.EndpointName)
                            .Where(
                                t => 
                                    t.Time > startSlice && 
                                    t.Time <= now)
                            .OrderBy(t => t.Time)
                            .Select(t => new {t.Id, t.Time})
                            .Statistics(out stats);
                        do
                        {
                            results.AddRange(query
                                                 .Skip(skip)
                                                 .Take(1024)
                                                 .ToList()
                                                 .Select(arg => new Tuple<string, DateTime>(arg.Id, arg.Time)));

                            skip += 1024;
                        } while (skip < stats.TotalResults &&
                                 ++numberOfRequestsExecutedSoFar < session.Advanced.MaxNumberOfRequestsPerSession);
                    }
                } while (skip < stats.TotalResults);

                using (var session = OpenSession())
                {
                    session.Advanced.AllowNonAuthoritativeInformation = true;

                    //Retrieve next time we need to run query
                    var startOfNextChunk =
                        session.Query<TimeoutData>("RavenTimeoutPersistence/TimeoutDataSortedByTime")
                            .Where(
                                t =>
                                t.OwningTimeoutManager == String.Empty ||
                                t.OwningTimeoutManager == Configure.EndpointName)
                            .Where(t => t.Time > now)
                            .OrderBy(t => t.Time)
                            .Select(t => new {t.Id, t.Time})
                            .FirstOrDefault();

                    if (startOfNextChunk != null)
                    {
                        nextTimeToRunQuery = startOfNextChunk.Time;
                    }
                    else
                    {
                        nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(10);
                    }

                    return results;
                }
            }
            catch (Exception)
            {
                if ((store == null) || (string.IsNullOrWhiteSpace(store.Identifier)) ||
                    (string.IsNullOrWhiteSpace(store.Url)))
                {
                    Logger.Error(
                        "Exception occurred while trying to access Raven Database. You can check Raven availability at its console at http://localhost:8080/raven/studio.html (unless Raven defaults were changed), or make sure the Raven service is running at services.msc (services programs console).");
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

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            using (var session = OpenSession())
            {
                timeoutData = session.Load<TimeoutData>(timeoutId);

                if (timeoutData == null)
                    return false;

                timeoutData.Time = DateTime.UtcNow.AddYears(-1);
                session.SaveChanges();

                session.Delete(timeoutData);
                session.SaveChanges();

                return true;
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