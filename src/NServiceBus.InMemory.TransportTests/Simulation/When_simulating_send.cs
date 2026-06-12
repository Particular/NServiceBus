namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using static InMemoryBrokerSimulationTestHelper;

[TestFixture]
public class When_simulating_send_delay
{
    [Test]
    public async Task Should_wait_for_simulated_time_to_advance()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = fakeTime,
            Send = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(5) } }
        });

        var dispatcher = await CreateDispatcher(broker, CancellationToken.None);
        await Dispatch(dispatcher, "msg-1", "queue", CancellationToken.None);

        var secondDispatch = Dispatch(dispatcher, "msg-2", "queue", CancellationToken.None);
        Assert.That(secondDispatch.IsCompleted, Is.False);

        fakeTime.Advance(TimeSpan.FromSeconds(5));
        await AsyncSpinWait.Until(() => secondDispatch.IsCompleted, maxIterations: 100);

        Assert.That(secondDispatch.IsCompleted, Is.True);
        await secondDispatch;

        Assert.That(broker.GetOrCreateQueue("queue").Count, Is.EqualTo(2));
    }
}
