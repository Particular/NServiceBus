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
    /// The identifier of the current trigger message. Trigger messages with a different identifier are stale.
    /// </summary>
    public required string TriggerId { get; set; }

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
