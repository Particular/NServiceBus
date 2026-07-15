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
    /// Defaults to <see cref="TraceConnector.ChildSpan"/>: receivers continue the trace.
    /// Can be overridden per message via <c>StartNewTraceOnReceive</c>
    /// or <c>ContinueExistingTraceOnReceive</c>.
    /// </summary>
    public TraceConnector SentMessageTraceConnector { get; set; } = TraceConnector.ChildSpan;

    /// <summary>
    /// Controls how the receive-side processing span relates to the publish span for events published by this endpoint.
    /// Defaults to <see cref="TraceConnector.SpanLink"/>: receivers start a new trace linked back to the publish span.
    /// Can be overridden per message via <c>StartNewTraceOnReceive</c>
    /// or <c>ContinueExistingTraceOnReceive</c>.
    /// </summary>
    public TraceConnector PublishedMessageTraceConnector { get; set; } = TraceConnector.SpanLink;
}
