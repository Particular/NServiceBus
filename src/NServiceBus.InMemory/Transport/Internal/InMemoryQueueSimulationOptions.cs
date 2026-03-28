namespace NServiceBus;

using System;

public sealed class InMemoryQueueSimulationOptions
{
    public TimeProvider? TimeProvider { get; set; }

    public InMemorySimulationOptions Default { get; } = new();

    public InMemorySimulationOptions Send { get; } = new();

    public InMemorySimulationOptions Receive { get; } = new();

    public InMemorySimulationOptions DelayedDelivery { get; } = new();
}
