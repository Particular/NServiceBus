namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

public sealed class InMemoryBrokerOptions
{
    public TimeProvider? TimeProvider { get; init; }

    public InMemorySimulationOptions Default { get; } = new();

    public InMemorySimulationOptions Send { get; } = new();

    public InMemorySimulationOptions Receive { get; } = new();

    public InMemorySimulationOptions DelayedDelivery { get; } = new();

    public InMemoryQueueSimulationOptions ForQueue(string queue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        return queues.GetOrAdd(queue, static _ => new InMemoryQueueSimulationOptions());
    }

    internal bool TryGetQueue(string queue, [NotNullWhen(true)] out InMemoryQueueSimulationOptions? options) => queues.TryGetValue(queue, out options);

    readonly ConcurrentDictionary<string, InMemoryQueueSimulationOptions> queues = new(StringComparer.OrdinalIgnoreCase);
}