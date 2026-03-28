namespace NServiceBus;

using System;

public sealed class InMemorySimulationOptions
{
    public TimeProvider? TimeProvider { get; set; }

    public InMemorySimulationMode? Mode { get; set; }

    public InMemoryRateLimitOptions? RateLimit { get; set; }
}