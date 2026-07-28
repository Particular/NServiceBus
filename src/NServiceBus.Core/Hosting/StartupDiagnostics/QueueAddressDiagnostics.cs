#nullable enable

namespace NServiceBus;

using System.Collections.Generic;

sealed class QueueAddressDiagnostics
{
    public required string BaseAddress { get; init; }
    public string? Discriminator { get; init; }
    public required Dictionary<string, string> Properties { get; init; }
    public string? Qualifier { get; init; }
}
