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
    /// Suppresses the "Start dispatching" and "Finished dispatching" activity events
    /// added to the incoming message span when outgoing messages are dispatched.
    /// Useful when these events add ingestion cost without diagnostic value.
    /// </summary>
    public bool SuppressDispatchingActivityEvents { get; set; }
}
