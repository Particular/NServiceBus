namespace NServiceBus;

using System;

public sealed class InMemoryRateLimitOptions
{
    public required int PermitLimit { get; init; }

    public required TimeSpan Window { get; init; }
}