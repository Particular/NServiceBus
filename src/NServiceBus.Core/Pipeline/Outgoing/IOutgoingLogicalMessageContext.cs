#nullable enable

namespace NServiceBus.Pipeline;

using System;
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
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7906",
        ReplacementTypeOrMember = "UpdateMessage<T>(T)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic overload instead.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    void UpdateMessage(object newInstance);

    /// <summary>
    /// Updates the message instance while preserving the specified message type.
    /// </summary>
    /// <typeparam name="T">The type used to update the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="newInstance">The replacement message instance.</param>
    [OverloadResolutionPriority(-1)]
    void UpdateMessage<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T newInstance) => UpdateMessage(newInstance!, typeof(T));

    /// <summary>
    /// Updates the message instance with the specified message type. The declared type controls how the message is routed and the message type header recorded on the message.
    /// </summary>
    /// <param name="newInstance">The replacement message instance. Must be assignable to <paramref name="messageType" />.</param>
    /// <param name="messageType">The declared logical message type. It can differ from the runtime type of <paramref name="newInstance" /> as long as the instance is assignable to it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="newInstance" /> or <paramref name="messageType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="newInstance" /> is not assignable to <paramref name="messageType" />.</exception>
    /// <remarks>
    /// Third-party implementations that inherit this default implementation fall back to the object overload and route by the runtime type of <paramref name="newInstance" />. Override this method to preserve a declared <paramref name="messageType" /> that differs from the runtime type.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = MessageOperations.DefaultInterfaceTrimmingSuppressionJustification)]
    void UpdateMessage(object newInstance, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        MessageTypeValidator.Validate(newInstance, messageType);
        UpdateMessage(newInstance);
    }
}