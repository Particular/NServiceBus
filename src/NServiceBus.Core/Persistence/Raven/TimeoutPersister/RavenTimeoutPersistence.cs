namespace NServiceBus.Persistence.Raven.TimeoutPersister
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using global::Raven.Client;
    using global::Raven.Client.Linq;
    using Logging;
    using Timeout.Core;

    [ObsoleteEx]
    public class RavenTimeoutPersistence : IPersistTimeouts
    {
        readonly IDocumentStore store;

        public RavenTimeoutPersistence(StoreAccessor  storeAccessor)
        {
            store = storeAccessor.Store;
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

                        var query = session.Query<TimeoutData>()
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
                        session.Query<TimeoutData>()
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