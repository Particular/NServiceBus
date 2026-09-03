#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Particular.Obsoletes;
using Pipeline;

class IncomingLogicalMessageContext : IncomingContext, IIncomingLogicalMessageContext
{
    internal IncomingLogicalMessageContext(LogicalMessage logicalMessage, IIncomingPhysicalMessageContext parentContext)
        : this(logicalMessage, parentContext.MessageId, parentContext.ReplyToAddress, parentContext.Message.Headers, parentContext)
    {
    }

    public IncomingLogicalMessageContext(LogicalMessage logicalMessage, string messageId, string replyToAddress, Dictionary<string, string> headers, IBehaviorContext parentContext)
        : base(messageId, replyToAddress, headers, parentContext)
    {
        Message = logicalMessage;
        Headers = headers;
        Set(logicalMessage);
    }

    public LogicalMessage Message { get; }

    public Dictionary<string, string> Headers { get; }

    public bool MessageHandled { get; set; }

    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7906",
        ReplacementTypeOrMember = "UpdateMessageInstance<T>(T)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic overload instead.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    public void UpdateMessageInstance(object newInstance)
    {
        ArgumentNullException.ThrowIfNull(newInstance);
        var sameInstance = ReferenceEquals(Message.Instance, newInstance);

        Message.Instance = newInstance;

        if (sameInstance)
        {
            return;
        }

        var factory = Builder.GetRequiredService<LogicalMessageFactory>();
        var newLogicalMessage = factory.Create(newInstance);

        Message.Metadata = newLogicalMessage.Metadata;
    }

    /// <summary>
    /// Updates the message instance contained in <see cref="LogicalMessage" /> while preserving the specified message type.
    /// </summary>
    /// <typeparam name="T">The type used to update the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="newInstance">The replacement message instance.</param>
    [OverloadResolutionPriority(-1)]
    public void UpdateMessageInstance<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T newInstance)
        => UpdateMessageInstance(newInstance!, typeof(T));

    /// <summary>
    /// Updates the message instance contained in <see cref="LogicalMessage" /> with the specified message type. The declared type controls how the message is routed and the message type header recorded on the message.
    /// </summary>
    /// <param name="newInstance">The replacement message instance. Must be assignable to <paramref name="messageType" />.</param>
    /// <param name="messageType">The declared logical message type. It can differ from the runtime type of <paramref name="newInstance" /> as long as the instance is assignable to it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="newInstance" /> or <paramref name="messageType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="newInstance" /> is not assignable to <paramref name="messageType" />.</exception>
    public void UpdateMessageInstance(object newInstance, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        ArgumentNullException.ThrowIfNull(newInstance);
        ArgumentNullException.ThrowIfNull(messageType);
        MessageTypeValidator.Validate(newInstance, messageType);

        var sameInstance = ReferenceEquals(Message.Instance, newInstance);

        Message.Instance = newInstance;

        if (sameInstance && Message.Metadata.MessageType == messageType)
        {
            return;
        }

        var factory = Builder.GetRequiredService<LogicalMessageFactory>();
        var newLogicalMessage = factory.Create(messageType, newInstance);

        Message.Metadata = newLogicalMessage.Metadata;
    }
}