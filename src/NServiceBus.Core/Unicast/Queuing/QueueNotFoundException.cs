namespace NServiceBus.Unicast.Queuing;

using System;

/// <summary>
/// Thrown when the queue could not be found.
/// </summary>
public class QueueNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="QueueNotFoundException" />.
    /// </summary>
    public QueueNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="QueueNotFoundException" />.
    /// </summary>
    public QueueNotFoundException(string queue, string message, Exception inner) : base(message, inner)
    {
        Queue = queue;
    }

    /// <summary>
    /// The queue address.
    /// </summary>
    public string Queue { get; set; }
}