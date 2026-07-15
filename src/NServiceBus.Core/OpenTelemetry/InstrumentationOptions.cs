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
    /// Controls whether the "Start dispatching" and "Finished dispatching" activity events
    /// are added to the incoming message span when outgoing messages are dispatched.
    /// Enabled by default for backward compatibility. Disable to avoid the ingestion cost
    /// of these events when they add no diagnostic value.
    /// </summary>
    public bool EmitMessageDispatchingEvents { get; set; } = true;
}
