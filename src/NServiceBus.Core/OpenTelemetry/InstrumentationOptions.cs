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
