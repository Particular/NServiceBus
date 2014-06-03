namespace NServiceBus.Persistence.Raven.TimeoutPersister
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using Timeout.Core;
    using Logging;
    using Raven;
    using global::Raven.Client;
    using global::Raven.Client.Linq;

    public class RavenTimeoutPersistence : IPersistTimeouts
    {
        readonly IDocumentStore store;

        public TimeSpan CleanupGapFromTimeslice { get; set; }
        public TimeSpan TriggerCleanupEvery { get; set; }
        DateTime lastCleanupTime = DateTime.MinValue;

        public RavenTimeoutPersistence(StoreAccessor storeAccessor)
        {
            store = storeAccessor.Store;
            TriggerCleanupEvery = TimeSpan.FromMinutes(2);
            CleanupGapFromTimeslice = TimeSpan.FromMinutes(1);
        }

        private static IRavenQueryable<TimeoutData> GetChunkQuery(IDocumentSession session)
        {
            session.Advanced.AllowNonAuthoritativeInformation = true;
            return session.Query<TimeoutData>()
                .OrderBy(t => t.Time)
                .Where(
                    t =>
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

                var skip = 0;
                var numberOfRequestsExecutedSoFar = 0;
                RavenQueryStatistics stats;
                do
                {
                    using (var session = OpenSession())
                    {
                        session.Advanced.AllowNonAuthoritativeInformation = true;

                        var query = GetChunkQuery(session)
                            .Where(
                                t =>
                                    t.Time > startSlice &&
                                    t.Time <= now)
                            .Select(t => new { t.Id, t.Time })
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

                // Set next execution to be now if we received stale results.
                // Delay the next execution a bit if we results weren't stale and we got the full chunk.
                if (stats.IsStale)
                {
                    nextTimeToRunQuery = now;
                }
                else
                {
                    using (var session = OpenSession())
                    {
                        var beginningOfNextChunk = GetChunkQuery(session)
                        .Where(t => t.Time > now)
                        .Take(1)
                        .Select(t => t.Time)
                        .FirstOrDefault();

                        nextTimeToRunQuery = (beginningOfNextChunk == default(DateTime))
                            ? DateTime.UtcNow.AddMinutes(10)
                            : beginningOfNextChunk.ToUniversalTime();
                    }
                }

                return results;
            }
            catch (WebException ex)
            {
                LogRavenConnectionFailure(ex);
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
            session.Advanced.UseOptimisticConcurrency = true;

            return session;
        }

        void LogRavenConnectionFailure(Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Raven could not be contacted. We tried to access Raven using the following url: {0}.",
                            store.Url);
            sb.AppendLine();
            sb.AppendFormat("Please ensure that you can open the Raven Studio by navigating to {0}.", store.Url);
            sb.AppendLine();
            sb.AppendLine(
                @"To configure NServiceBus to use a different Raven connection string add a connection string named ""NServiceBus.Persistence"" in your config file, example:");
            sb.AppendFormat(
                @"<connectionStrings>
    <add name=""NServiceBus.Persistence"" connectionString=""http://localhost:9090"" />
</connectionStrings>");
            sb.AppendLine("Original exception: " + exception);

            Logger.Warn(sb.ToString());
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(RavenTimeoutPersistence));
    }
}