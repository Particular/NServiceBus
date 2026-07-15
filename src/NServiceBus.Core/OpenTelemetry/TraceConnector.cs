#nullable enable

namespace NServiceBus;

/// <summary>
/// Controls how the receive-side processing span relates to the outgoing send or publish span.
/// </summary>
public enum TraceConnector
{
    /// <summary>
    /// The receiving endpoint continues the trace: the processing span becomes a child of the outgoing span.
    /// </summary>
    ChildSpan,

    /// <summary>
    /// The receiving endpoint starts a new trace: the processing span becomes the root of a new trace with a link back to the outgoing span.
    /// </summary>
    SpanLink
}
