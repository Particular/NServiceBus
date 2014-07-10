namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using NServiceBus.Persistence.Raven;
    using NServiceBus.Persistence.Raven.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Embedded;

    [TestFixture]
    public class RavenTimeoutPersisterTests
    {
        [TestCase, Repeat(200)]
        public void Should_not_skip_timeouts()
        {
            var startSlice = DateTime.UtcNow.AddYears(-10);
            // avoid cleanup from running during the test by making it register as being run
            Assert.AreEqual(0, persister.GetCleanupChunk(startSlice).Count());
            Assert.IsFalse(persister.SeenStaleResults);

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
                                            Time = DateTime.UtcNow.AddSeconds(RandomProvider.GetThreadRandom().Next(1, 20)),
                                            OwningTimeoutManager = string.Empty,
                                        };
                               persister.Add(td);
                               expected.Add(new Tuple<string, DateTime>(td.Id, td.Time));
                               lastExpectedTimeout = (td.Time > lastExpectedTimeout) ? td.Time : lastExpectedTimeout;
                               //Trace.WriteLine("Added timeout for " + td.Time);
                           }
                           finishedAdding = true;
                           Trace.WriteLine("*** Finished adding ***");
                       }).Start();

            // Mimic the behavior of the TimeoutPersister coordinator
            var found = 0;
            TimeoutData tempTd;
            while (!finishedAdding || startSlice < lastExpectedTimeout)
            {
                Trace.WriteLine("Querying for timeouts starting at " + startSlice + " with last known added timeout at " + lastExpectedTimeout);
                DateTime nextRetrieval;
                var timeoutDatas = persister.GetNextChunk(startSlice, out nextRetrieval);
                foreach (var timeoutData in timeoutDatas)
                {
                    if (startSlice < timeoutData.Item2)
                    {
                        startSlice = timeoutData.Item2;
                    }

                    Trace.WriteLine("Deleting " + timeoutData.Item1);
                    //Assert.IsTrue(persister.TryRemove(timeoutData.Item1, out tempTd)); // Raven returns duplicates, so we can't assert on this here
                    if (persister.TryRemove(timeoutData.Item1, out tempTd))
                        found++;
                }
            }

            WaitForIndexing(documentStore);

            // If the persister reports stale results have been seen at one point during its normal operation,
            // we need to perform manual cleaup.
            if (persister.SeenStaleResults)
            {
                while (true)
                {
                    var chunkToCleanup = persister.GetCleanupChunk(DateTime.UtcNow.Add(persister.CleanupGapFromTimeslice)).ToArray();
                    Trace.WriteLine("Cleanup: got a chunk of size " + chunkToCleanup.Length);
                    if (chunkToCleanup.Length == 0) break;

                    found += chunkToCleanup.Length;
                    foreach (var tuple in chunkToCleanup)
                    {
                        Assert.IsTrue(persister.TryRemove(tuple.Item1, out tempTd));
                    }

                    WaitForIndexing(documentStore);
                }
            }
            else
            {
                Trace.WriteLine("** Haven't seen stale results **");
            }

            using (var session = documentStore.OpenSession())
            {
                var results = session.Query<TimeoutData>().ToList();
                Assert.AreEqual(0, results.Count);
            }

            Assert.AreEqual(expected.Count, found);            
        }

        [TestCase]
        [Ignore("Deadlocks")]
        public void Should_not_skip_timeouts_also_with_multiple_clients_adding_timeouts()
        {
            var expected = new List<Tuple<string, DateTime>>();
            var lastTimeout = DateTime.UtcNow;
            var finishedAdding1 = false;
            var finishedAdding2 = false;
            new Thread(() =>
            {
                var sagaId = Guid.NewGuid();
                for (var i = 0; i < 10000; i++)
                {
                    var td = new TimeoutData
                    {
                        SagaId = sagaId,
                        Destination = new Address("queue", "machine"),
                        Time = DateTime.UtcNow.AddSeconds(RandomProvider.GetThreadRandom().Next(1, 20)),
                        OwningTimeoutManager = string.Empty,
                    };
                    persister.Add(td);
                    expected.Add(new Tuple<string, DateTime>(td.Id, td.Time));
                    lastTimeout = (td.Time > lastTimeout) ? td.Time : lastTimeout;
                    //Trace.WriteLine("Added timeout for " + td.Time);
                }
                finishedAdding1 = true;
                Trace.WriteLine("*** Finished adding ***");
            }).Start();

            new Thread(() =>
            {
                using (var store = new EmbeddableDocumentStore
                {
                    RunInMemory = true
                }.Initialize())
                {
                    var persister2 = new RavenTimeoutPersistence(new StoreAccessor(store));

                    var sagaId = Guid.NewGuid();
                    for (var i = 0; i < 10000; i++)
                    {
                        var td = new TimeoutData
                        {
                            SagaId = sagaId,
                            Destination = new Address("queue", "machine"),
                            Time = DateTime.UtcNow.AddSeconds(1 + RandomProvider.GetThreadRandom().Next(1, 20)),
                            OwningTimeoutManager = string.Empty,
                        };
                        persister2.Add(td);
                        expected.Add(new Tuple<string, DateTime>(td.Id, td.Time));
                        lastTimeout = (td.Time > lastTimeout) ? td.Time : lastTimeout;
                        //Trace.WriteLine("Added timeout for " + td.Time);
                    }
                }
                finishedAdding2 = true;
                Trace.WriteLine("*** Finished adding via a second client connection ***");
            }).Start();

            // Mimic the behavior of the TimeoutPersister coordinator
            var found = 0;
            var startSlice = DateTime.UtcNow.AddYears(-10);
            while (!finishedAdding1 || !finishedAdding2 || startSlice < lastTimeout)
            {
                DateTime nextRetrieval;
                var timeoutDatas = persister.GetNextChunk(startSlice, out nextRetrieval);
                Trace.WriteLine("Querying for timeouts starting at " + startSlice + " with last known added timeout at " + lastTimeout);
                foreach (var timeoutData in timeoutDatas)
                {
                    if (startSlice < timeoutData.Item2)
                    {
                        startSlice = timeoutData.Item2;
                    }
                    found++;

                    TimeoutData td;
                    Assert.IsTrue(persister.TryRemove(timeoutData.Item1, out td));
                }
            }

            Assert.AreEqual(expected.Count, found);

            WaitForIndexing(documentStore);

            using (var session = documentStore.OpenSession())
            {
                var results = session.Query<TimeoutData>().ToList();
                Assert.AreEqual(0, results.Count);
            }
        }

        IDocumentStore documentStore;
        RavenTimeoutPersistence persister;

        [SetUp]
        public void SetUp()
        {
            documentStore = new EmbeddableDocumentStore
                            {
                                RunInMemory = true
                            }.Initialize();

            persister = new RavenTimeoutPersistence(new StoreAccessor(documentStore))
                        {
                            TriggerCleanupEvery = TimeSpan.FromHours(1), // Make sure cleanup doesn't run automatically
                        };
        }

        [TearDown]
        public void TearDown()
        {
            documentStore.Dispose();
        }

        static void WaitForIndexing(IDocumentStore store, string db = null, TimeSpan? timeout = null)
        {
            var databaseCommands = store.DatabaseCommands;
            if (db != null)
                databaseCommands = databaseCommands.ForDatabase(db);
            var spinUntil = SpinWait.SpinUntil(() => databaseCommands.GetStatistics().StaleIndexes.Length == 0, timeout ?? TimeSpan.FromSeconds(20));
            Assert.True(spinUntil);
        }

        public static class RandomProvider
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
