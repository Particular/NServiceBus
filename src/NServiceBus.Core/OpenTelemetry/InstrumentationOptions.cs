#nullable enable

namespace NServiceBus;

/// <summary>
/// Controls opt-in OpenTelemetry instrumentation behaviors.
/// Accessed via <c>endpointConfiguration.Tracing()</c>.
/// </summary>
public class InstrumentationOptions
{
    /// <summary>
    /// Appends the destination to span names following the OTel messaging convention
    /// <c>{messaging.operation.name} {destination}</c>, e.g. "process orders" or "send payments".
    /// Disabled by default for backward compatibility.
    /// </summary>
    public bool UseMessageDestinationInSpanNames { get; set; }

    /// <summary>
    /// Controls instrumentation of the recoverability pipeline (retries and error handling).
    /// </summary>
    public RecoverabilityInstrumentationOptions Recoverability { get; } = new();

    /// <summary>
    /// Controls instrumentation of explicitly delayed messages (<c>SendOptions.DelayDeliveryWith</c>
    /// / <c>DoNotDeliverBefore</c> and saga timeouts. 
    /// Recoverability-driven delayed retries are controlled separately via
    /// <see cref="RecoverabilityInstrumentationOptions.DelayedRetryTraceMode"/>.
    /// </summary>
    public DelayedDeliveryInstrumentationOptions DelayedDelivery { get; } = new();

    /// <summary>
    /// Controls whether the "Start dispatching" and "Finished dispatching" activity events
    /// are added to the incoming message span when outgoing messages are dispatched.
    /// Enabled by default for backward compatibility. Disable to avoid the ingestion cost
    /// of these events when they add no diagnostic value.
    /// </summary>
    public bool EmitMessageDispatchingEvents { get; set; } = true;

    /// <summary>
    /// Controls how the receive-side processing span relates to the send span for messages sent by this endpoint.
    /// Defaults to <see cref="TraceMode.ContinueExisting"/>: receivers continue the trace.
    /// Can be overridden per message via <see cref="OpenTelemetryExtensions.StartNewTraceOnReceive(SendOptions)"/>
    /// or <see cref="OpenTelemetryExtensions.ContinueExistingTraceOnReceive(SendOptions)"/>.
    /// </summary>
    public TraceMode SendTraceMode { get; set; } = TraceMode.ContinueExisting;

    /// <summary>
    /// Controls how the receive-side processing span relates to the publish span for events published by this endpoint.
    /// Defaults to <see cref="TraceMode.StartNew"/>: receivers start a new trace linked back to the publish span.
    /// Can be overridden per message via <see cref="OpenTelemetryExtensions.StartNewTraceOnReceive(PublishOptions)"/>
    /// or <see cref="OpenTelemetryExtensions.ContinueExistingTraceOnReceive(PublishOptions)"/>.
    /// </summary>
    public TraceMode PublishTraceMode { get; set; } = TraceMode.StartNew;
}

/// <summary>
/// Controls instrumentation of the recoverability pipeline (retries and error handling).
/// </summary>
public class RecoverabilityInstrumentationOptions
{
    /// <summary>
    /// Controls how the span for a delayed retry relates to the failed
    /// attempt's trace.
    /// </summary>
    public TraceMode DelayedRetryTraceMode { get; set; } = TraceMode.StartNew;
}

/// <summary>
/// Controls instrumentation of delayed messages.
/// </summary>
public class DelayedDeliveryInstrumentationOptions
{
    /// <summary>
    /// Controls how a delayed <c>Send</c> relates to the sender's trace, when requested directly
    /// by application code via <c>SendOptions.DelayDeliveryWith</c>/<c>DoNotDeliverBefore</c>.
    /// </summary>
    public TraceMode SendOperationTraceMode { get; set; } = TraceMode.StartNew;

    /// <summary>
    /// Controls how a saga timeout (<c>Saga.RequestTimeout</c>) relates to the trace of the
    /// message that requested it.
    /// </summary>
    public TraceMode SagaTimeoutTraceMode { get; set; } = TraceMode.StartNew;
}
