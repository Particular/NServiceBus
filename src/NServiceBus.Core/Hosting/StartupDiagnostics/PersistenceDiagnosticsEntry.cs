#nullable enable

namespace NServiceBus;

sealed class PersistenceDiagnosticsEntry
{
    public required string Type { get; init; }
    public required string Version { get; init; }
}
