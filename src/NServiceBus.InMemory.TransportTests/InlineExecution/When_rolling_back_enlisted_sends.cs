#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Buffers;
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public class When_rolling_back_enlisted_sends
{
    [Test]
    public void Should_return_pending_envelope_buffers_to_pool()
    {
        var pool = new TrackingArrayPool();
        var transaction = CreateReceiveTransaction();

        transaction.Enlist(CreateEnvelope(pool, [1]));
        transaction.Enlist(CreateEnvelope(pool, [2]));

        transaction.Rollback();

        Assert.That(pool.ReturnedBuffers, Is.EqualTo(2));
        Assert.That(GetPendingEnvelopes(transaction), Is.Empty);
    }

    static BrokerEnvelope CreateEnvelope(TrackingArrayPool pool, byte[] body)
    {
        var buffer = pool.Rent(body.Length);
        body.CopyTo(buffer, 0);

        return new BrokerEnvelope(
            "message-id",
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
