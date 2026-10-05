namespace NServiceBus.Routing;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents a route that should deliver the message to all interested subscribers.
/// </summary>
public class MulticastAddressTag : AddressTag
{
    /// <summary>
    /// Initializes a new instance of <see cref="MulticastAddressTag" />.
    /// </summary>
    /// <param name="messageType">The event being published.</param>
    public MulticastAddressTag([DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        MessageType = messageType;
    }

    /// <summary>
    /// The event being published.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)]
    public Type MessageType { get; }
}