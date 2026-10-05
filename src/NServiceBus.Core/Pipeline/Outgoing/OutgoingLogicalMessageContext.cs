#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Particular.Obsoletes;
using Pipeline;
using Routing;

class OutgoingLogicalMessageContext : OutgoingContext, IOutgoingLogicalMessageContext
{
    public OutgoingLogicalMessageContext(string messageId, Dictionary<string, string> headers, OutgoingLogicalMessage message, IReadOnlyCollection<RoutingStrategy> routingStrategies, IBehaviorContext parentContext)
        : base(messageId, headers, parentContext)
    {
        Message = message;
        RoutingStrategies = routingStrategies;
        Set(message);
    }

    public OutgoingLogicalMessage Message { get; private set; }

    public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; }

    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7906",
        ReplacementTypeOrMember = "UpdateMessage<T>(T)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic overload instead.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    public void UpdateMessage(object newInstance)
    {
        ArgumentNullException.ThrowIfNull(newInstance);

        if (Message.Instance != newInstance)
        {
            Message = new OutgoingLogicalMessage(newInstance.GetType(), newInstance);
        }
    }

    /// <summary>
    /// Replaces the current message with the provided typed message instance.
    /// </summary>
    /// <typeparam name="T">The type used to update the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="newInstance">The new message instance.</param>
    [OverloadResolutionPriority(-1)]
    public void UpdateMessage<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T newInstance)
    {
        ArgumentNullException.ThrowIfNull(newInstance);

        if (Message.Instance != (object)newInstance || Message.MessageType != typeof(T))
        {
            Message = new OutgoingLogicalMessage(typeof(T), newInstance);
        }
    }

    public void UpdateMessage(object newInstance, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        ArgumentNullException.ThrowIfNull(newInstance);
        ArgumentNullException.ThrowIfNull(messageType);
        MessageTypeValidator.Validate(newInstance, messageType);

        if (Message.Instance != newInstance || Message.MessageType != messageType)
        {
            Message = new OutgoingLogicalMessage(messageType, newInstance);
        }
    }
}