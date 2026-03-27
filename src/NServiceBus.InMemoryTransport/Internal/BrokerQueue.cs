namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public sealed class InMemoryChannel
{
    readonly Channel<BrokerEnvelope> channel = Channel.CreateUnbounded<BrokerEnvelope>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false
    });

    public ValueTask Enqueue(BrokerEnvelope envelope, CancellationToken cancellationToken = default)
    {
        return channel.Writer.WriteAsync(envelope, cancellationToken);
    }

    public ValueTask<BrokerEnvelope> Dequeue(CancellationToken cancellationToken = default)
    {
        return channel.Reader.ReadAsync(cancellationToken);
    }

    public bool TryPeek(out BrokerEnvelope? envelope)
    {
        return channel.Reader.TryPeek(out envelope);
    }

    public int Count => channel.Reader.Count;

    public ChannelReader<BrokerEnvelope> Reader => channel.Reader;
    public ChannelWriter<BrokerEnvelope> Writer => channel.Writer;
}
