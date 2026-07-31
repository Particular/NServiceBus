#nullable enable

namespace NServiceBus.Pipeline;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Particular.Obsoletes;
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
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7892",
        ReplacementTypeOrMember = "UpdateMessage<T>(T)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic overload instead.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    void UpdateMessage(object newInstance);

    /// <summary>
    /// Updates the message instance while preserving the specified message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="newInstance">The replacement message instance.</param>
    [OverloadResolutionPriority(-1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = MessageOperations.DefaultInterfaceTrimmingSuppressionJustification)]
    void UpdateMessage<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T newInstance)
    {
        UpdateMessage(newInstance!);
    }
}