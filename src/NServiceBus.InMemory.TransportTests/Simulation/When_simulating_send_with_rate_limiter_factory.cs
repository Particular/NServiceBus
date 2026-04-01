namespace NServiceBus.TransportTests;

using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class When_simulating_send_with_rate_limiter_factory
{
    [Test]
    public async Task Should_create_and_reuse_limiter_per_operation_and_queue()
    {
        var factoryCalls = 0;
        var createdLimiters = new List<CountingRateLimiter>();
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            Send =
            {
                Mode = InMemorySimulationMode.Reject,
                RateLimiterFactory = _ =>
                {
                    factoryCalls++;
                    var limiter = new CountingRateLimiter(permitCount: 1);
                    createdLimiters.Add(limiter);
                    return limiter;
                }
            }
        });

        var dispatcher = await InMemoryBrokerSimulationTestHelper.CreateDispatcher(broker);
        await InMemoryBrokerSimulationTestHelper.Dispatch(dispatcher, "msg-1", "queue");
        Assert.ThrowsAsync<InMemorySimulationException>(async () => await InMemoryBrokerSimulationTestHelper.Dispatch(dispatcher, "msg-2", "queue"));

        await InMemoryBrokerSimulationTestHelper.Dispatch(dispatcher, "msg-3", "other-queue");
        Assert.ThrowsAsync<InMemorySimulationException>(async () => await InMemoryBrokerSimulationTestHelper.Dispatch(dispatcher, "msg-4", "other-queue"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(factoryCalls, Is.EqualTo(2));
            Assert.That(createdLimiters.Count, Is.EqualTo(2));
            Assert.That(createdLimiters[0].AttemptAcquireCalls, Is.EqualTo(2));
            Assert.That(createdLimiters[1].AttemptAcquireCalls, Is.EqualTo(2));
        }

        foreach (var limiter in createdLimiters)
        {
            await limiter.DisposeAsync();
        }
    }
}