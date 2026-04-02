namespace NServiceBus.TransportTests;

static class BrokerTestExtensions
{
    public static int DrainQueue(this InMemoryBroker broker, string address)
    {
        if (!broker.TryGetQueue(address, out var queue) || queue == null)
        {
            return 0;
        }

        var drained = 0;
        while (queue.TryDequeue(out var envelope) && envelope != null)
        {
            envelope.Dispose();
            drained++;
        }

        return drained;
    }
}
