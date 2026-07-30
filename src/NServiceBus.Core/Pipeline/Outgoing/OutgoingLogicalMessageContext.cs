#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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