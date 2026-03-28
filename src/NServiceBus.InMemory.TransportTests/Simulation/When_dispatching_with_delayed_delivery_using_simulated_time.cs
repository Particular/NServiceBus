namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NServiceBus.DelayedDelivery;
using NServiceBus.Transport;
using NUnit.Framework;
using Routing;
using static InMemoryBrokerSimulationTestHelper;

[TestFixture]
public class When_dispatching_with_delayed_delivery_using_simulated_time
{
    [Test]
    public async Task Should_schedule_relative_delivery_from_broker_time()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions { TimeProvider = simulatedTime });

        var dispatcher = await CreateDispatcher(broker);
        var message = new OutgoingMessage("msg-1", [], new byte[] { 1 });
        var properties = new DispatchProperties
        {
            DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(5))
        };

        await dispatcher.Dispatch(
            new TransportOperations(new TransportOperation(message, new UnicastAddressTag("queue"), properties)),
            new TransportTransaction(),
            CancellationToken.None);

        var dequeued = broker.TryDequeueDelayed(simulatedTime.GetUtcNow().AddSeconds(4), out var tooEarlyEnvelope);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(dequeued, Is.False);
            Assert.That(tooEarlyEnvelope, Is.Null);
        }

        dequeued = broker.TryDequeueDelayed(simulatedTime.GetUtcNow().AddSeconds(5), out var dueEnvelope);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(dequeued, Is.True);
            Assert.That(dueEnvelope, Is.Not.Null);
        }
    }
}
