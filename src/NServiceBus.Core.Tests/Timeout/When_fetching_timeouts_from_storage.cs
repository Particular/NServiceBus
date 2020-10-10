namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;
    using AcceptanceTesting;

    [TestFixture]
    public class When_fetching_timeouts_from_storage_with_inMemory
    {
        AcceptanceTestingTimeoutPersister persister;

        [SetUp]
        public void Setup()
        {
             persister = new AcceptanceTestingTimeoutPersister(() => DateTime.UtcNow);
        }

        [Test]
        public async Task Should_only_return_timeouts_for_time_slice()
        {
            const int numberOfTimeoutsToAdd = 10;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                await persister.Add(new TimeoutData
                {
                    OwningTimeoutManager = string.Empty,
                    Time = DateTime.UtcNow.AddHours(-1)
                }, new ContextBag());
            }

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                await persister.Add(new TimeoutData
                {
                    OwningTimeoutManager = string.Empty,
                    Time = DateTime.UtcNow.AddHours(1)
                }, new ContextBag());
            }

            var nextChunk = await GetNextChunk();

            Assert.AreEqual(numberOfTimeoutsToAdd, nextChunk.DueTimeouts.Count());
        }

        [Test]
        public async Task Should_set_the_next_run()
        {
            const int numberOfTimeoutsToAdd = 50;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                var d = new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(-1),
                    OwningTimeoutManager = "MyEndpoint"
                };

                await persister.Add(d, new ContextBag());
            }

            var expected = DateTime.UtcNow.AddHours(1);
            await persister.Add(new TimeoutData
            {
                Time = expected,
                OwningTimeoutManager = string.Empty,
            }, new ContextBag());

            var nextChunk = await GetNextChunk();

            var totalMilliseconds = (expected - nextChunk.NextTimeToQuery).Duration().TotalMilliseconds;

            Assert.True(totalMilliseconds < 200);
        }

        Task<TimeoutsChunk> GetNextChunk()
        {
            return persister.GetNextChunk(DateTime.UtcNow.AddYears(-3));
        }
    }
}