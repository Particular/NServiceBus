namespace NServiceBus;

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

    public ValueTask Enqueue(BrokerEnvelope envelope, CancellationToken cancellationToken = default) => channel.Writer.WriteAsync(envelope, cancellationToken);

    public ValueTask<BrokerEnvelope> Dequeue(CancellationToken cancellationToken = default) => channel.Reader.ReadAsync(cancellationToken);

    public ValueTask<bool> WaitToRead(CancellationToken cancellationToken = default) => channel.Reader.WaitToReadAsync(cancellationToken);

    public bool TryDequeue(out BrokerEnvelope? envelope) => channel.Reader.TryRead(out envelope);

    public bool TryPeek(out BrokerEnvelope? envelope) => channel.Reader.TryPeek(out envelope);

    public bool TryComplete() => channel.Writer.TryComplete();

    public int Count => channel.Reader.Count;

    public ChannelReader<BrokerEnvelope> Reader => channel.Reader;
    public ChannelWriter<BrokerEnvelope> Writer => channel.Writer;
}
