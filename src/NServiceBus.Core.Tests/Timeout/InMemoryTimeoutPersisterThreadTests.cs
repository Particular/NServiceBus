namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using NServiceBus.Extensibility;
    using NServiceBus.InMemory.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class InMemoryTimeoutPersisterThreadTests
    {

        ConcurrentDictionary<int, Guid> sagaIdGuids = new ConcurrentDictionary<int, Guid>();

        [Test]
        [Explicit]
        public void Run()
        {
            var stopwatch = Stopwatch.StartNew();
            var options = new TimeoutPersistenceOptions(new ContextBag());
            var inMemoryTimeoutPersister = new InMemoryTimeoutPersister();

            for (var i = 0; i < 10; i++)
            {
                var thread = new Thread(() => Runner(inMemoryTimeoutPersister, options));
                thread.Start();
                thread.Join();
            }

            Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);
        }

        void Runner(InMemoryTimeoutPersister inMemoryTimeoutPersister, TimeoutPersistenceOptions options)
        {
            for (var i = 0; i < 10000; i++)
            {
                GetNextChunk(inMemoryTimeoutPersister);
                Add(inMemoryTimeoutPersister, options);
                GetNextChunk(inMemoryTimeoutPersister);
                TryRemove(inMemoryTimeoutPersister, options);
                GetNextChunk(inMemoryTimeoutPersister);
                RemoveTimeoutBy(inMemoryTimeoutPersister, options);
                GetNextChunk(inMemoryTimeoutPersister);
            }
        }

        void RemoveTimeoutBy(IPersistTimeouts inMemoryTimeoutPersister, TimeoutPersistenceOptions options)
        {
            var sagaId = sagaIdGuids.GetOrAdd(Thread.CurrentThread.ManagedThreadId, new Guid());
            inMemoryTimeoutPersister.RemoveTimeoutBy(sagaId, options);
        }

        static void TryRemove(IPersistTimeouts inMemoryTimeoutPersister, TimeoutPersistenceOptions options)
        {
            TimeoutData timeout;
            inMemoryTimeoutPersister.TryRemove(Thread.CurrentThread.Name, options, out timeout);
        }

        static void Add(IPersistTimeouts inMemoryTimeoutPersister, TimeoutPersistenceOptions options)
        {
            inMemoryTimeoutPersister.Add(new TimeoutData
            {
                Time = DateTime.Now,
                Id = Thread.CurrentThread.Name
            }, options);
        }

        static void GetNextChunk(IQueryTimeouts inMemoryTimeoutPersister)
        {
            for (var i = 0; i < 10; i++)
            {
                DateTime nextTimeToRunQuery;
                inMemoryTimeoutPersister.GetNextChunk(DateTime.MinValue, out nextTimeToRunQuery).ToList();   
            }
        }
    }

}