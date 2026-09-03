#nullable enable

namespace NServiceBus;

/// <summary>
/// Controls how the receive-side processing span relates to the outgoing send or publish span.
/// </summary>
public enum TraceMode
{
    /// <summary>
    /// The receiving endpoint continues the trace: the processing span becomes a child of the outgoing span.
    /// </summary>
    ContinueExisting,

    /// <summary>
    /// The receiving endpoint starts a new trace: the processing span becomes the root of a new trace with a link back to the outgoing span.
    /// </summary>
    StartNew,

    /// <summary>
    /// The receiving endpoint ignores the trace carried in the message headers: the processing span becomes a child of the ambient
    /// <c>Activity.Current</c> at receive time (for example a transport or host span), or the root of a new trace when there is none,
    /// with a link back to the outgoing span.
    /// </summary>
    UseExisting,
}
