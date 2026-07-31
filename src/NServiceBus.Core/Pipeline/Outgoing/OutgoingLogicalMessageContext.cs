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

    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7892",
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

    [OverloadResolutionPriority(-1)]
    public void UpdateMessage<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T newInstance)
    {
        ArgumentNullException.ThrowIfNull(newInstance);

        if (Message.Instance != (object)newInstance || Message.MessageType != typeof(T))
        {
            Message = new OutgoingLogicalMessage(typeof(T), newInstance);
        }
    }
}