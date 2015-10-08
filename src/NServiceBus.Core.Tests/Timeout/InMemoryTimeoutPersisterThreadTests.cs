namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using InMemory.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class InMemoryTimeoutPersisterThreadTests
    {
        [Test]
        [Explicit]
        public void Run()
        {
            var stopwatch = Stopwatch.StartNew();
            var inMemoryTimeoutPersister = new InMemoryTimeoutPersister();

            for (var i = 0; i < 10; i++)
            {
                var thread = new Thread(() => Runner(inMemoryTimeoutPersister, new ContextBag()).Wait());
                thread.Start();
                thread.Join();
            }

            Console.Out.WriteLine(stopwatch.ElapsedMilliseconds);
        }

        async Task Runner(InMemoryTimeoutPersister inMemoryTimeoutPersister, ReadOnlyContextBag context)
        {
            for (var i = 0; i < 10000; i++)
            {
                await GetNextChunk(inMemoryTimeoutPersister);
                await Add(inMemoryTimeoutPersister, context);
                await GetNextChunk(inMemoryTimeoutPersister);
                await TryRemove(inMemoryTimeoutPersister, context);
                await GetNextChunk(inMemoryTimeoutPersister);
                await RemoveTimeoutBy(inMemoryTimeoutPersister, context);
                await GetNextChunk(inMemoryTimeoutPersister);
            }
        }

        Task RemoveTimeoutBy(IPersistTimeouts inMemoryTimeoutPersister, ReadOnlyContextBag context)
        {
            var sagaId = sagaIdGuids.GetOrAdd(Thread.CurrentThread.ManagedThreadId, new Guid());
            return inMemoryTimeoutPersister.RemoveTimeoutBy(sagaId, context);
        }

        static async Task TryRemove(IPersistTimeouts inMemoryTimeoutPersister, ReadOnlyContextBag context)
        {
            await inMemoryTimeoutPersister.Remove(Thread.CurrentThread.Name, context);
        }

        static Task Add(IPersistTimeouts inMemoryTimeoutPersister, ReadOnlyContextBag context)
        {
            return inMemoryTimeoutPersister.Add(new TimeoutData
            {
                Time = DateTime.Now,
                Id = Thread.CurrentThread.Name
            }, context);
        }

        static async Task GetNextChunk(IQueryTimeouts inMemoryTimeoutPersister)
        {
            for (var i = 0; i < 10; i++)
            {
                (await inMemoryTimeoutPersister.GetNextChunk(DateTime.MinValue)).DueTimeouts.ToList();
            }
        }

        ConcurrentDictionary<int, Guid> sagaIdGuids = new ConcurrentDictionary<int, Guid>();
    }
}