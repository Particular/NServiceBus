#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_committing_enlisted_sends_through_dispatcher
{
    [Test]
    public async Task Should_return_original_envelope_buffer_to_pool_after_dispatch()
    {
        await using var broker = new InMemoryBroker();
        var pool = new TrackingArrayPool();
        var dispatcher = new CapturingDispatcher();
        var runner = new InlineExecutionRunner(
            "input",
            TransportTransactionMode.SendsAtomicWithReceive,
            static (_, _, _) => { },
            broker,
            static () => CancellationToken.None);

        runner.SetDispatcher(dispatcher);
        runner.Initialize(
            (messageContext, _) =>
            {
                var pendingEnvelope = CreateEnvelope(pool, [1, 2, 3]);
                messageContext.TransportTransaction.Get<IInMemoryReceiveTransaction>().Enlist(pendingEnvelope);
                return Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled));

        var receivedEnvelope = BrokerPayloadStore.Borrow(
            "received",
            [4],
            new Dictionary<string, string>(),
            "input",
            isPublished: false,
            sequenceNumber: 1);

        await runner.Process(receivedEnvelope);
        receivedEnvelope.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(dispatcher.CapturedBody, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(pool.ReturnedBuffers, Is.EqualTo(1));
        });
    }

    static BrokerEnvelope CreateEnvelope(TrackingArrayPool pool, byte[] body)
    {
        var buffer = pool.Rent(body.Length);
        body.CopyTo(buffer, 0);

        return new BrokerEnvelope(
            "pending",
            new ReadOnlyMemory<byte>(buffer, 0, body.Length),
            new Dictionary<string, string>(),
            "destination",
            false,
            1)
        {
            Pool = pool,
            Buffer = buffer
        };
    }

    sealed class CapturingDispatcher : IMessageDispatcher
    {
        public byte[]? CapturedBody { get; private set; }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            CapturedBody = outgoingMessages.UnicastTransportOperations[0].Message.Body.ToArray();
            return Task.CompletedTask;
        }
    }

    sealed class TrackingArrayPool : ArrayPool<byte>
    {
        public int ReturnedBuffers { get; private set; }

        public override byte[] Rent(int minimumLength) => new byte[minimumLength];

        public override void Return(byte[] array, bool clearArray = false)
        {
            ReturnedBuffers++;
            if (clearArray)
            {
                Array.Clear(array);
            }
        }
    }
}
