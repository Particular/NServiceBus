#nullable enable

namespace NServiceBus.Performance.TimeToBeReceived;

using System;

/// <summary>
/// Instructs the transport to discard the message if it hasn't been received
/// within the specified <see cref="TimeSpan"/>.
/// </summary>
/// <remarks>
/// Initializes the constraint with a max time.
/// </remarks>
public class DiscardIfNotReceivedBefore(TimeSpan maxTime)
{

    /// <summary>
    /// The max time to wait before discarding the message.
    /// </summary>
    public TimeSpan MaxTime { get; } = maxTime;
}