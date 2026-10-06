#nullable enable

namespace NServiceBus;

// Mirrors the shape of InstrumentationOptions (endpointConfiguration.Tracing()) so the
// diagnostics output reads like the configuration API.
sealed class OpenTelemetryDiagnostics
{
    public required string SendTraceMode { get; init; }
    public required string PublishTraceMode { get; init; }
    public required string ExceptionRecordingMode { get; init; }
    public required RecoverabilityInstrumentationDiagnostics Recoverability { get; init; }
    public required DelayedDeliveryInstrumentationDiagnostics DelayedDelivery { get; init; }

    // Remove together with V11BehaviorSwitch in obsoletes-v10.cs.
    public required bool UseV11Behavior { get; init; }
}

sealed class RecoverabilityInstrumentationDiagnostics
{
    public required string DelayedRetryTraceMode { get; init; }
}

sealed class DelayedDeliveryInstrumentationDiagnostics
{
    public required string SendOperationTraceMode { get; init; }
    public required string SagaTimeoutTraceMode { get; init; }
}
