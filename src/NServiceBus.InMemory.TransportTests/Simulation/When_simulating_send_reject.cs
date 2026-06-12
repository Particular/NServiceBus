namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using NUnit.Framework;
using static InMemoryBrokerSimulationTestHelper;

[TestFixture]
public class When_simulating_send_reject
{
    [Test]
    public void Should_throw_immediately()
    {
        Assert.DoesNotThrowAsync(async () =>
        {
            await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
            {
                Send = { Mode = InMemorySimulationMode.Reject, RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromMinutes(1) } }
            });

            var dispatcher = await CreateDispatcher(broker, CancellationToken.None);
            await Dispatch(dispatcher, "msg-1", "queue", CancellationToken.None);
        });

        Assert.ThrowsAsync<InMemorySimulationException>(async () =>
        {
            await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
            {
                Send = { Mode = InMemorySimulationMode.Reject, RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromMinutes(1) } }
            });

            var dispatcher = await CreateDispatcher(broker, CancellationToken.None);
            await Dispatch(dispatcher, "msg-1", "queue", CancellationToken.None);
            await Dispatch(dispatcher, "msg-2", "queue", CancellationToken.None);
        });
    }
}
