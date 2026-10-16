#nullable enable

namespace NServiceBus.Sagas;

using System;

/// <summary>
/// A pending saga timeout as stored by a persister that supports timeout management.
/// </summary>
public sealed class StoredSagaTimeout
{
    /// <summary>
    /// The identifier of the timeout. It doesn't change when the timeout is rescheduled.
    /// </summary>
    public required string TimeoutId { get; set; }

    /// <summary>
    /// The identifier of the current timeout message, used as its message ID. Timeout messages with a different message ID are stale.
    /// </summary>
    public required string TriggerId { get; set; }

    /// <summary>
    /// Starts at 1 when the timeout is requested and increases by one on each reschedule, so the platform can apply changes in order.
    /// </summary>
    public required int Version { get; set; }

    /// <summary>
    /// The last requested due time.
    /// </summary>
    public required DateTimeOffset DueTime { get; set; }

    /// <summary>
    /// The full name of the timeout message type.
    /// </summary>
    public required string MessageType { get; set; }

    /// <summary>
    /// The content type of the serialized payload.
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// The serialized timeout message.
    /// </summary>
    public required byte[] Payload { get; set; }
}
