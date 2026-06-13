namespace NServiceBus.TransportTests;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Transport;
using NUnit.Framework;
using static InMemoryBrokerSimulationTestHelper;

[TestFixture]
public class When_simulated_send_rejects_during_broker_dispatch
{
    [Test]
    public async Task Should_return_payload_buffer_when_broker_dispatch_rejects()
    {
        var pool = new TrackingArrayPool<byte>();
        var buffer = pool.Rent(1);
        buffer[0] = 1;
        var headers = new Dictionary<string, string>();
        var envelope = new BrokerEnvelope("msg-1", new ReadOnlyMemory<byte>(buffer, 0, 1), headers, "queue", false, 1)
        {
            Pool = pool,
            Buffer = buffer
        };

        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            Send = { Mode = InMemorySimulationMode.Reject, RateLimiter = new CountingRateLimiter(0) }
        });

        var dispatcher = await CreateDispatcher(broker, CancellationToken.None);

        Assert.ThrowsAsync<InMemorySimulationException>(async () =>
        {
            await DispatchToBroker(dispatcher, "queue", envelope, CancellationToken.None);
        });

        Assert.That(pool.ReturnCount, Is.EqualTo(1));
    }

    static Task DispatchToBroker(IMessageDispatcher dispatcher, string destination, BrokerEnvelope envelope, CancellationToken cancellationToken)
    {
        var dispatchToBroker = dispatcher.GetType().GetMethod("DispatchToBroker", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)dispatchToBroker.Invoke(dispatcher, [destination, envelope.MessageId, envelope, null, cancellationToken])!;
    }

    sealed class TrackingArrayPool<T> : ArrayPool<T>
    {
        readonly ArrayPool<T> inner = Shared;
        int returnCount;

        public int ReturnCount => Volatile.Read(ref returnCount);

        public override T[] Rent(int minimumLength) => inner.Rent(minimumLength);

        public override void Return(T[] array, bool clearArray = false)
        {
            Interlocked.Increment(ref returnCount);
            inner.Return(array, clearArray);
        }
    }
}
