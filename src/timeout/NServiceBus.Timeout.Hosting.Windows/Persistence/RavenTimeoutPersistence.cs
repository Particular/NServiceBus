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
        readonly IDocumentStore store;

        public TimeSpan CleanupGapFromTimeslice { get; set; }
        public TimeSpan TriggerCleanupEvery { get; set; }
        DateTime lastCleanupTime = DateTime.MinValue;

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

            TriggerCleanupEvery = TimeSpan.FromMinutes(2);
            CleanupGapFromTimeslice = TimeSpan.FromMinutes(1);            
        }

        private static IRavenQueryable<TimeoutData> GetChunkQuery(IDocumentSession session)
        {
            session.Advanced.AllowNonAuthoritativeInformation = true;
            return session.Query<TimeoutData>("RavenTimeoutPersistence/TimeoutDataSortedByTime")
                .OrderBy(t => t.Time)
                .Where(t =>
                    t.OwningTimeoutManager == String.Empty ||
                    t.OwningTimeoutManager == Configure.EndpointName);
        }

        public IEnumerable<Tuple<string, DateTime>> GetCleanupChunk(DateTime startSlice)
        {
            using (var session = OpenSession())
            {
                var chunk = GetChunkQuery(session)
                    .Where(t => t.Time <= startSlice.Subtract(CleanupGapFromTimeslice))
                    .Select(t => new
                    {
                        t.Id,
                        t.Time
                    })
                    .Take(1024)
                    .ToList()
                    .Select(arg => new Tuple<string, DateTime>(arg.Id, arg.Time));

                lastCleanupTime = DateTime.UtcNow;

                return chunk;
            }
        }

        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            try
            {
                var now = DateTime.UtcNow;
                var results = new List<Tuple<string, DateTime>>();

                // Allow for occasionally cleaning up old timeouts for edge cases where timeouts have been
                // added after startSlice have been set to a later timout and we might have missed them
                // because of stale indexes.
                if (lastCleanupTime.Add(TriggerCleanupEvery) > now || lastCleanupTime == DateTime.MinValue)
                {
                    results.AddRange(GetCleanupChunk(startSlice));
                }

                DateTime beginningOfNextChunk;
                RavenQueryStatistics stats;
                using (var session = OpenSession())
                {
                    var query = GetChunkQuery(session)
                        .Where(
                            t =>
                                t.Time > startSlice &&
                                t.Time <= now)
                        .Select(t => new { t.Id, t.Time })
                        .Statistics(out stats)
                        .Take(1024);

                    results.AddRange(query
                        .ToList()
                        .Select(arg => new Tuple<string, DateTime>(arg.Id, arg.Time)));

                    beginningOfNextChunk = GetChunkQuery(session)
                        .Where(t => t.Time > now)
                        .Take(1)
                        .Select(t => t.Time)
                        .FirstOrDefault();
                }

                nextTimeToRunQuery = (beginningOfNextChunk == default(DateTime))
                    ? nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(10)
                    : beginningOfNextChunk.ToUniversalTime();

                // Set next execution to be now if we haven't consumed the entire thing or received stale results.
                // Delay the next execution a bit if we results weren't stale and we got the full chunk.
                if (stats.TotalResults > 1024 || stats.IsStale)
                {
                    nextTimeToRunQuery = now;
                }

                return results;
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