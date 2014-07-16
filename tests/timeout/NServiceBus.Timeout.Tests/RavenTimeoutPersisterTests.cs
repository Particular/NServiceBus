using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NServiceBus.Timeout.Core;
using NServiceBus.Timeout.Hosting.Windows.Persistence;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Database;

namespace NServiceBus.Timeout.Tests
{
    public class RavenTimeoutPersisterTests
    {
        static RavenTimeoutPersisterTests()
        {
            Configure.GetEndpointNameAction = () => string.Empty;
        }

        [TestCase]
        public void Should_not_skip_timeouts()
        {
            var db = Guid.NewGuid().ToString();
            documentStore = new DocumentStore
            {
                Url = "http://localhost:8080",
                DefaultDatabase = db,
            }.Initialize();
            persister = new RavenTimeoutPersistence(documentStore)
            {
                TriggerCleanupEvery = TimeSpan.FromHours(1), // Make sure cleanup doesn't run automatically
            };

            var startSlice = DateTime.UtcNow.AddYears(-10);
            // avoid cleanup from running during the test by making it register as being run
            Assert.AreEqual(0, persister.GetCleanupChunk(startSlice).Count());

            var expected = new List<Tuple<string, DateTime>>();
            var lastExpectedTimeout = DateTime.UtcNow;
            var finishedAdding = false;

            new Thread(() =>
            {
                var sagaId = Guid.NewGuid();
                for (var i = 0; i < 10000; i++)
                {
                    var td = new TimeoutData
                    {
                        SagaId = sagaId,
                        Destination = new Address("queue", "machine"),
                        Time = DateTime.UtcNow.AddSeconds(RandomProvider.GetThreadRandom().Next(5, 20)),
                        OwningTimeoutManager = string.Empty,
                    };
                    persister.Add(td);
                    expected.Add(new Tuple<string, DateTime>(td.Id, td.Time));
                    lastExpectedTimeout = (td.Time > lastExpectedTimeout) ? td.Time : lastExpectedTimeout;
                }
                finishedAdding = true;
                Console.WriteLine("*** Finished adding ***");
            }).Start();

            // Mimic the behavior of the TimeoutPersister coordinator
            var found = 0;
            TimeoutData tempTd;
            while (!finishedAdding || startSlice < lastExpectedTimeout)
            {
                DateTime nextRetrieval;
                var timeoutDatas = persister.GetNextChunk(startSlice, out nextRetrieval);
                foreach (var timeoutData in timeoutDatas)
                {
                    if (startSlice < timeoutData.Item2)
                    {
                        startSlice = timeoutData.Item2;
                    }

                    if (persister.TryRemove(timeoutData.Item1, out tempTd))
                        found++;
                }
            }

            WaitForIndexing(documentStore);

            // If the persister reports stale results have been seen at one point during its normal operation,
            // we need to perform manual cleaup.
            while (true)
            {
                var chunkToCleanup = persister.GetCleanupChunk(DateTime.UtcNow.AddDays(1)).ToArray();
                Console.WriteLine("Cleanup: got a chunk of size " + chunkToCleanup.Length);
                if (chunkToCleanup.Length == 0) break;

                found += chunkToCleanup.Length;
                foreach (var tuple in chunkToCleanup)
                {
                    Assert.IsTrue(persister.TryRemove(tuple.Item1, out tempTd));
                }

                WaitForIndexing(documentStore);
            }

            using (var session = documentStore.OpenSession())
            {
                var results = session.Query<TimeoutData>().ToList();
                Assert.AreEqual(0, results.Count);
            }

            Assert.AreEqual(expected.Count, found);
        }

        [TestCase]
        public void Should_not_skip_timeouts_also_with_multiple_clients_adding_timeouts()
        {
            var db = Guid.NewGuid().ToString();
            documentStore = new DocumentStore
            {
                Url = "http://localhost:8080",
                DefaultDatabase = db,
            }.Initialize();
            persister = new RavenTimeoutPersistence(documentStore)
            {
                TriggerCleanupEvery = TimeSpan.FromDays(1), // Make sure cleanup doesn't run automatically
            };

            var startSlice = DateTime.UtcNow.AddYears(-10);
            // avoid cleanup from running during the test by making it register as being run
            Assert.AreEqual(0, persister.GetCleanupChunk(startSlice).Count());

            const int insertsPerThread = 10000;
            var expected1 = new List<Tuple<string, DateTime>>();
            var expected2 = new List<Tuple<string, DateTime>>();
            var lastExpectedTimeout = DateTime.UtcNow;
            var finishedAdding1 = false;
            var finishedAdding2 = false;

            new Thread(() =>
            {
                var sagaId = Guid.NewGuid();
                for (var i = 0; i < insertsPerThread; i++)
                {
                    var td = new TimeoutData
                    {
                        SagaId = sagaId,
                        Destination = new Address("queue", "machine"),
                        Time = DateTime.UtcNow.AddSeconds(RandomProvider.GetThreadRandom().Next(1, 20)),
                        OwningTimeoutManager = string.Empty,
                    };
                    persister.Add(td);
                    expected1.Add(new Tuple<string, DateTime>(td.Id, td.Time));
                    lastExpectedTimeout = (td.Time > lastExpectedTimeout) ? td.Time : lastExpectedTimeout;
                }
                finishedAdding1 = true;
                Console.WriteLine("*** Finished adding ***");
            }).Start();

            new Thread(() =>
            {
                using (var store = new DocumentStore
                {
                    Url = "http://localhost:8080",
                    DefaultDatabase = db,
                }.Initialize())
                {
                    var persister2 = new RavenTimeoutPersistence(store);

                    var sagaId = Guid.NewGuid();
                    for (var i = 0; i < insertsPerThread; i++)
                    {
                        var td = new TimeoutData
                        {
                            SagaId = sagaId,
                            Destination = new Address("queue", "machine"),
                            Time = DateTime.UtcNow.AddSeconds(RandomProvider.GetThreadRandom().Next(1, 20)),
                            OwningTimeoutManager = string.Empty,
                        };
                        persister2.Add(td);
                        expected2.Add(new Tuple<string, DateTime>(td.Id, td.Time));
                        lastExpectedTimeout = (td.Time > lastExpectedTimeout) ? td.Time : lastExpectedTimeout;
                    }
                }
                finishedAdding2 = true;
                Console.WriteLine("*** Finished adding via a second client connection ***");
            }).Start();

            // Mimic the behavior of the TimeoutPersister coordinator
            var found = 0;
            TimeoutData tempTd;
            while (!finishedAdding1 || !finishedAdding2 || startSlice < lastExpectedTimeout)
            {
                DateTime nextRetrieval;
                var timeoutDatas = persister.GetNextChunk(startSlice, out nextRetrieval);
                foreach (var timeoutData in timeoutDatas)
                {
                    if (startSlice < timeoutData.Item2)
                    {
                        startSlice = timeoutData.Item2;
                    }

                    if(persister.TryRemove(timeoutData.Item1, out tempTd))
                        found++;
                }
            }

            WaitForIndexing(documentStore);

            // If the persister reports stale results have been seen at one point during its normal operation,
            // we need to perform manual cleaup.
            while (true)
            {
                var chunkToCleanup = persister.GetCleanupChunk(DateTime.UtcNow.AddDays(1)).ToArray();
                Console.WriteLine("Cleanup: got a chunk of size " + chunkToCleanup.Length);
                if (chunkToCleanup.Length == 0) break;

                found += chunkToCleanup.Length;
                foreach (var tuple in chunkToCleanup)
                {
                    Assert.IsTrue(persister.TryRemove(tuple.Item1, out tempTd));
                }

                WaitForIndexing(documentStore);
            }

            using (var session = documentStore.OpenSession())
            {
                var results = session.Query<TimeoutData>().ToList();
                Assert.AreEqual(0, results.Count);
            }

            Assert.AreEqual(expected1.Count + expected2.Count, found);
        }

        IDocumentStore documentStore;
        RavenTimeoutPersistence persister;

        [TearDown]
        public void TearDown()
        {
            if (documentStore != null)
                documentStore.Dispose();
        }

        public static void WaitForIndexing(IDocumentStore store, string db = null, TimeSpan? timeout = null)
        {
            var databaseCommands = store.DatabaseCommands;
            if (db != null)
                databaseCommands = databaseCommands.ForDatabase(db);
            var spinUntil = SpinWait.SpinUntil(() => databaseCommands.GetStatistics().StaleIndexes.Length == 0, timeout ?? TimeSpan.FromSeconds(20));
            Assert.True(spinUntil);
        }

        static class RandomProvider
        {
            private static int seed = Environment.TickCount;

            private static ThreadLocal<Random> randomWrapper = new ThreadLocal<Random>(() =>
                new Random(Interlocked.Increment(ref seed))
            );

            public static Random GetThreadRandom()
            {
                return randomWrapper.Value;
            }
        }
    }
}
