namespace NServiceBus.Routing;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A routing strategy for multicast routing.
/// </summary>
public class MulticastRoutingStrategy : RoutingStrategy
{
    /// <summary>
    /// Creates new routing strategy.
    /// </summary>
    public MulticastRoutingStrategy([DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        this.messageType = messageType;
    }

    /// <summary>
    /// Applies the routing strategy to the message.
    /// </summary>
    /// <param name="headers">Message headers.</param>
    public override AddressTag Apply(Dictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        return new MulticastAddressTag(messageType);
    }

    [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)]
    readonly Type messageType;
}