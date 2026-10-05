#nullable enable

namespace NServiceBus;

sealed class EndpointDiagnostics
{
    public required string Name { get; init; }
    public required bool SendOnly { get; init; }
    public required string NServiceBusVersion { get; init; }
}
