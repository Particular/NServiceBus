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
    /// Promotes public properties of message instances to span attributes
    /// as <c>nservicebus.message.{PropertyName}</c>.
    /// Defaults to <see cref="MessagePayloadAsTag.None"/>. May expose sensitive data and incurs reflection cost.
    /// </summary>
    public MessagePayloadAsTag MessagePayloadAsTag { get; set; }
}

/// <summary>
/// Controls which message payloads are promoted to span attributes.
/// </summary>
public enum MessagePayloadAsTag
{
    /// <summary>No message properties are promoted to span tags.</summary>
    None,

    /// <summary>Public properties of the incoming message instance are promoted to span tags.</summary>
    IncomingMessage,

    /// <summary>Public properties of outgoing message instances are promoted to span tags.</summary>
    OutgoingMessage,

    /// <summary>Public properties of both incoming and outgoing message instances are promoted to span tags.</summary>
    All
}
