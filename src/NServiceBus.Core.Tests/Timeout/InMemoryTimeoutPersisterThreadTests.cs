namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
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
                var thread = new Thread(() => Runner(inMemoryTimeoutPersister, options).Wait());
                thread.Start();
                thread.Join();
            }

            Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);
        }

        async Task Runner(InMemoryTimeoutPersister inMemoryTimeoutPersister, TimeoutPersistenceOptions options)
        {
            for (var i = 0; i < 10000; i++)
            {
                await GetNextChunk(inMemoryTimeoutPersister);
                await Add(inMemoryTimeoutPersister, options);
                await GetNextChunk(inMemoryTimeoutPersister);
                await TryRemove(inMemoryTimeoutPersister, options);
                await GetNextChunk(inMemoryTimeoutPersister);
                await RemoveTimeoutBy(inMemoryTimeoutPersister, options);
                await GetNextChunk(inMemoryTimeoutPersister);
            }
        }

        Task RemoveTimeoutBy(IPersistTimeouts inMemoryTimeoutPersister, TimeoutPersistenceOptions options)
        {
            var sagaId = sagaIdGuids.GetOrAdd(Thread.CurrentThread.ManagedThreadId, new Guid());
            return inMemoryTimeoutPersister.RemoveTimeoutBy(sagaId, options);
        }

        static async Task TryRemove(IPersistTimeouts inMemoryTimeoutPersister, TimeoutPersistenceOptions options)
        {
            await inMemoryTimeoutPersister.Remove(Thread.CurrentThread.Name, options);
        }

        static Task Add(IPersistTimeouts inMemoryTimeoutPersister, TimeoutPersistenceOptions options)
        {
            return inMemoryTimeoutPersister.Add(new TimeoutData
            {
                Time = DateTime.Now,
                Id = Thread.CurrentThread.Name
            }, options);
        }

        static async Task GetNextChunk(IQueryTimeouts inMemoryTimeoutPersister)
        {
            for (var i = 0; i < 10; i++)
            {
                (await inMemoryTimeoutPersister.GetNextChunk(DateTime.MinValue)).DueTimeouts.ToList();   
            }
        }
    }

}