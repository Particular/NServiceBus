namespace NServiceBus;

using System;
using System.Threading.RateLimiting;

public sealed class InMemorySimulationOptions
{
    public TimeProvider? TimeProvider { get; set; }

    public InMemorySimulationMode? Mode { get; set; }

    public InMemoryRateLimitOptions? RateLimit { get; set; }

    public RateLimiter? RateLimiter { get; set; }

    public Func<TimeProvider, RateLimiter>? RateLimiterFactory { get; set; }
}