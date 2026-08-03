namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Particular.Obsoletes;

/// <summary>
/// Syntactic sugar for <see cref="IMessageProcessingContext" />.
/// </summary>
public static class MessageProcessingContextExtensions
{
    /// <summary>
    /// Sends the message to the endpoint which sent the message currently being handled on this thread.
    /// </summary>
    /// <param name="context">Object being extended.</param>
    /// <param name="message">The message to send.</param>
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7892",
        ReplacementTypeOrMember = "Reply<T>(this IMessageProcessingContext, T)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic overload instead, or the overload accepting an explicit messageType when the static type is unavailable.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    public static Task Reply(this IMessageProcessingContext context, object message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        return context.Reply(message, new ReplyOptions());
    }

    /// <summary>
    /// Sends the typed message to the endpoint which sent the message currently being handled on this thread.
    /// </summary>
    /// <typeparam name="T">The type used to reply with the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="context">Object being extended.</param>
    /// <param name="message">The message to send.</param>
    [OverloadResolutionPriority(-1)]
    public static Task Reply<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(this IMessageProcessingContext context, T message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        return context.Reply<T>(message, new ReplyOptions());
    }

    /// <summary>
    /// Sends the message with the specified message type to the endpoint which sent the message currently being handled on this thread. The declared type controls how the message is routed and the message type header recorded on the message.
    /// </summary>
    /// <param name="context">Object being extended.</param>
    /// <param name="message">The message to send. Must be assignable to <paramref name="messageType" />.</param>
    /// <param name="messageType">The declared logical message type. It can differ from the runtime type of <paramref name="message" /> as long as the instance is assignable to it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> or <paramref name="messageType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="message" /> is not assignable to <paramref name="messageType" />.</exception>
    public static Task Reply(this IMessageProcessingContext context, object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        return context.Reply(message, messageType, new ReplyOptions());
    }

    /// <summary>
    /// Instantiates a message of type T and performs a regular Reply.
    /// </summary>
    /// <typeparam name="T">The type used to reply with the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="context">Object being extended.</param>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    public static Task Reply<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(this IMessageProcessingContext context, Action<T> messageConstructor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageConstructor);

        return context.Reply(messageConstructor, new ReplyOptions());
    }
}