using NServiceBus.Logging;

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

        public IEnumerable<TimeoutData> GetAll()
        {
            try
            {
                var skip = 0;
                var results = new List<TimeoutData>();
                var numberOfRequestsExecutedSoFar = 0;
                RavenQueryStatistics stats;
                var timeFetchWasRequested = DateTime.UtcNow;
                do
                {
                    using (var session = OpenSession())
                    {
                        var query = session.Query<TimeoutData>("RavenTimeoutPersistence/TimeoutDataSortedByTime")
                            // we'll wait for nonstale results up until the point in time that we start fetching
                            // since other timeouts that has arrived in the meantime will have been added to the 
                            // cache anyway. If we not do this there is a risk that we'll miss them and breaking their SLA
                            .Where(t => t.OwningTimeoutManager == "" || t.OwningTimeoutManager == Configure.EndpointName)
                            .Customize(c => c.WaitForNonStaleResultsAsOf(timeFetchWasRequested))
                            .Statistics(out stats);
                        do
                        {
                            results.AddRange(query.Skip(skip).Take(1024));
                            skip += 1024;
                        }
                        while (skip < stats.TotalResults && ++numberOfRequestsExecutedSoFar < session.Advanced.MaxNumberOfRequestsPerSession);
                    }
                } while (skip < stats.TotalResults);

                return results;
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

        public void ClearTimeoutsFor(Guid sagaId)
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

            return session;
        }

        static readonly ILog Logger = LogManager.GetLogger("RavenTimeoutPersistence");
    }
}