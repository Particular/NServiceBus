#nullable enable

namespace NServiceBus.Pipeline;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents a logical message about to be push out to the transport.
/// </summary>
public class OutgoingLogicalMessage
{
    /// <summary>
    /// Initializes the message with a explicit message type and instance. Use this constructor if the message type is
    /// different from the instance type.
    /// </summary>
    public OutgoingLogicalMessage([DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, object message)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(message);

        MessageType = messageType;
        Instance = message;
    }

    /// <summary>
    /// The <see cref="Type" /> of the message instance.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)]
    public Type MessageType { get; }

    /// <summary>
    /// The message instance.
    /// </summary>
    public object Instance { get; }
}