#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// A pending timeout of the saga instance handling the current message.
/// </summary>
/// <typeparam name="TTimeoutMessageType">The timeout message type.</typeparam>
public sealed class SagaTimeout<TTimeoutMessageType>
{
    internal SagaTimeout(string timeoutId, DateTimeOffset dueTime, TTimeoutMessageType payload)
    {
        TimeoutId = timeoutId;
        DueTime = dueTime;
        Payload = payload;
    }

    /// <summary>
    /// The identifier of the timeout. It doesn't change when the timeout is rescheduled.
    /// </summary>
    public string TimeoutId { get; }

    /// <summary>
    /// The last requested due time.
    /// </summary>
    public DateTimeOffset DueTime { get; }

    /// <summary>
    /// The timeout message passed to the timeout handler. Changes are stored only when the timeout is rescheduled.
    /// </summary>
    public TTimeoutMessageType Payload { get; }
}
