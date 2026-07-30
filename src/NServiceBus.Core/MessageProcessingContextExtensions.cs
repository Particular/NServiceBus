namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
    /// <typeparam name="T">The type of message, usually an interface.</typeparam>
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
    /// Sends the message with the specified message type to the endpoint which sent the message currently being handled on this thread.
    /// </summary>
    /// <param name="context">Object being extended.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The declared message type.</param>
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
    /// <typeparam name="T">The type of message, usually an interface.</typeparam>
    /// <param name="context">Object being extended.</param>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    public static Task Reply<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(this IMessageProcessingContext context, Action<T> messageConstructor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageConstructor);

        return context.Reply(messageConstructor, new ReplyOptions());
    }
}