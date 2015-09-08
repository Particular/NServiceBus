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
        InMemoryTimeoutPersister persister;
        TimeoutPersistenceOptions options;

        [SetUp]
        public void Setup()
        {
            options = new TimeoutPersistenceOptions(new ContextBag());
            persister = new InMemoryTimeoutPersister();
        }

        [Test]
        public async Task Should_remove_timeouts_by_id()
        {
            var t1 = new TimeoutData { Id = "1", Time = DateTime.UtcNow.AddHours(-1) };
            await persister.Add(t1, options);

            var t2 = new TimeoutData { Id = "2", Time = DateTime.UtcNow.AddHours(-1) };
            await persister.Add(t2, options);

            var firstChunk = await GetNextChunk();
            foreach (var timeout in firstChunk.DueTimeouts)
            {
                await persister.Remove(timeout.Id, options);
            }

            var secondChunk = await GetNextChunk();

            Assert.AreEqual(0, secondChunk.DueTimeouts.Count());
        }

        Task<TimeoutsChunk> GetNextChunk()
        {
            return persister.GetNextChunk(DateTime.UtcNow.AddYears(-3));
        }
    }
}
