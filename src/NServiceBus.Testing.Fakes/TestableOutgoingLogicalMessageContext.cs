namespace NServiceBus.Testing;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Pipeline;
using Routing;

/// <summary>
/// A testable implementation of <see cref="IOutgoingLogicalMessageContext" />.
/// </summary>
public partial class TestableOutgoingLogicalMessageContext : TestableOutgoingContext, IOutgoingLogicalMessageContext
{
    /// <summary>
    /// Updates the message instance.
    /// </summary>
    [RequiresUnreferencedCode(DynamicMemberTypeAccess.RuntimeTypeRoutingTrimmingMessage)]
    public virtual void UpdateMessage(object newInstance)
    {
        Message = new OutgoingLogicalMessage(newInstance.GetType(), newInstance);
    }

    /// <summary>
    /// Updates the message instance while preserving the specified message type.
    /// </summary>
    [OverloadResolutionPriority(-1)]
    public virtual void UpdateMessage<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T newInstance)
    {
        Message = new OutgoingLogicalMessage(typeof(T), newInstance!);
    }

    /// <summary>
    /// The outgoing message.
    /// </summary>
    public OutgoingLogicalMessage Message { get; set; } = new OutgoingLogicalMessage(typeof(object), new object());

    /// <summary>
    /// The routing strategies for this message.
    /// </summary>
    public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; } = System.Array.Empty<RoutingStrategy>();
}