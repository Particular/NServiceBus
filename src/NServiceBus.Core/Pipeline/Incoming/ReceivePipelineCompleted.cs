namespace NServiceBus;

using System;
using Transport;

/// <summary>
/// Event raised when a receive pipeline is completed.
/// </summary>
public class ReceivePipelineCompleted
{
    /// <summary>
    /// Constructs the event.
    /// </summary>
    public ReceivePipelineCompleted(IncomingMessage processedMessage, DateTimeOffset startedAt, DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(processedMessage);
        ArgumentNullException.ThrowIfNull(startedAt);
        ArgumentNullException.ThrowIfNull(completedAt);

        ProcessedMessage = processedMessage;
        StartedAt = startedAt;
        CompletedAt = completedAt;
    }

    /// <summary>
    /// The processed message.
    /// </summary>
    public IncomingMessage ProcessedMessage { get; }

    /// <summary>
    /// Time when the receive pipeline started.
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    /// Time when the receive pipeline completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; }
}