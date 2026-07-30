#nullable enable

namespace NServiceBus.Pipeline;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Routing;

/// <summary>
/// Outgoing pipeline context.
/// </summary>
public interface IOutgoingLogicalMessageContext : IOutgoingContext
{
    /// <summary>
    /// The outgoing message.
    /// </summary>
    OutgoingLogicalMessage Message { get; }

    /// <summary>
    /// The routing strategies for this message.
    /// </summary>
    IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; }

    /// <summary>
    /// Updates the message instance.
    /// </summary>
    void UpdateMessage(object newInstance);

    /// <summary>
    /// Updates the message instance while preserving the specified message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="newInstance">The replacement message instance.</param>
    [OverloadResolutionPriority(-1)]
    void UpdateMessage<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T newInstance)
    {
        UpdateMessage(newInstance!);
    }
}