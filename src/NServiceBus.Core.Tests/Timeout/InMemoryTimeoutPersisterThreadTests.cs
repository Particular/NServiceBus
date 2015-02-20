namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
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
            var inMemoryTimeoutPersister = new InMemoryTimeoutPersister();

            for (var i = 0; i < 10; i++)
            {
                var thread = new Thread(() => Runner(inMemoryTimeoutPersister));
                thread.Start();
                thread.Join();
            }

            Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);
        }

        void Runner(InMemoryTimeoutPersister inMemoryTimeoutPersister)
        {
            for (var i = 0; i < 10000; i++)
            {
                GetNextChunk(inMemoryTimeoutPersister);
                Add(inMemoryTimeoutPersister);
                GetNextChunk(inMemoryTimeoutPersister);
                TryRemove(inMemoryTimeoutPersister);
                GetNextChunk(inMemoryTimeoutPersister);
                RemoveTimeoutBy(inMemoryTimeoutPersister);
                GetNextChunk(inMemoryTimeoutPersister);
            }
        }

        void RemoveTimeoutBy(InMemoryTimeoutPersister inMemoryTimeoutPersister)
        {
            var sagaId = sagaIdGuids.GetOrAdd(Thread.CurrentThread.ManagedThreadId, new Guid());
            inMemoryTimeoutPersister.RemoveTimeoutBy(sagaId);
        }

        void TryRemove(InMemoryTimeoutPersister inMemoryTimeoutPersister)
        {
            TimeoutData timeout;
            inMemoryTimeoutPersister.TryRemove(Thread.CurrentThread.Name, out timeout);
        }

        void Add(InMemoryTimeoutPersister inMemoryTimeoutPersister)
        {
            inMemoryTimeoutPersister.Add(new TimeoutData
            {
                Time = DateTime.Now,
                Id = Thread.CurrentThread.Name
            });
        }

        void GetNextChunk(InMemoryTimeoutPersister inMemoryTimeoutPersister)
        {
            for (var i = 0; i < 10; i++)
            {
                DateTime nextTimeToRunQuery;
                inMemoryTimeoutPersister.GetNextChunk(DateTime.MinValue, out nextTimeToRunQuery).ToList();   
            }
        }
    }

}