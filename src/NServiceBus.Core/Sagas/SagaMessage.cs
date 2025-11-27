namespace NServiceBus.Sagas;

using System;

/// <summary>
/// Representation of a message that is related to a saga.
/// </summary>
public sealed record SagaMessage
{
    /// <summary>
    /// Creates a new instance of <see cref="SagaMessage" />.
    /// </summary>
    /// <param name="messageType">Type of the message.</param>
    /// <param name="isAllowedToStart"><code>true</code> if the message can start the saga, <code>false</code> otherwise.</param>
    /// <param name="isTimeout"><code>true</code> if the message is a timeout, <code>false</code> otherwise.</param>
    public SagaMessage(Type messageType, bool isAllowedToStart, bool isTimeout)
    {
        if (isAllowedToStart && isTimeout)
        {
            throw new ArgumentException("A timeout message cannot start a saga.");
        }

        MessageType = messageType;
        MessageTypeName = messageType.FullName;
        IsAllowedToStartSaga = isAllowedToStart;
        IsTimeout = isTimeout;
    }

    /// <summary>
    /// The type of the message.
    /// </summary>
    public Type MessageType { get; }

    /// <summary>
    /// The full name of the message type.
    /// </summary>
    public string MessageTypeName { get; }

    /// <summary>
    /// True if the message can start the saga.
    /// </summary>
    public bool IsAllowedToStartSaga { get; }

    /// <summary>
    /// True if the message is a timeout message.
    /// </summary>
    public bool IsTimeout { get; }
}