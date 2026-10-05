#nullable enable

namespace NServiceBus;

using System.Collections.Generic;

sealed class ReceivingDiagnostics
{
    public required QueueAddressDiagnostics LocalQueueAddress { get; init; }
    public QueueAddressDiagnostics? InstanceSpecificQueueAddress { get; init; }
    public required bool PurgeOnStartup { get; init; }
    public required string TransactionMode { get; init; }
    public required int MaxConcurrency { get; init; }
    public required SatelliteDiagnostics[] Satellites { get; init; }
    public required Dictionary<string, List<string>> MessageHandlers { get; init; }
}

sealed class SatelliteDiagnostics
{
    public required string Name { get; init; }
    public required QueueAddressDiagnostics ReceiveAddress { get; init; }
    public required int MaxConcurrency { get; init; }
}
