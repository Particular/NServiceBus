#nullable enable

namespace NServiceBus;

sealed class RecoverabilityDiagnostics
{
    public required int ImmediateRetries { get; init; }
    public required int DelayedRetries { get; init; }
    public required string DelayedRetriesTimeIncrease { get; init; }
    public required string ErrorQueue { get; init; }
    public required string[] UnrecoverableExceptions { get; init; }
}
