namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using InMemory.TimeoutPersister;
    using NServiceBus.Extensibility;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_removing_timeouts_from_the_storage_with_inMemory
    {
        [Test]
        public async Task Should_remove_timeouts_by_id()
        {
            var persister = new InMemoryTimeoutPersister();
            var t1 = new TimeoutData { Id = "1", Time = DateTime.UtcNow.AddHours(-1) };
            await persister.Add(t1, new ContextBag());

            var t2 = new TimeoutData { Id = "2", Time = DateTime.UtcNow.AddHours(-1) };
            await persister.Add(t2, new ContextBag());

            var firstChunk = await persister.GetNextChunk(DateTime.UtcNow.AddYears(-3));
            foreach (var timeout in firstChunk.DueTimeouts)
            {
                await persister.Remove(timeout.Id, new ContextBag());
            }

            var secondChunk = await persister.GetNextChunk(DateTime.UtcNow.AddYears(-3));

            Assert.AreEqual(0, secondChunk.DueTimeouts.Count());
        }
    }
}
