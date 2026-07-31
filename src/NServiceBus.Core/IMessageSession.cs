#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Particular.Obsoletes;

/// <summary>
/// A session which provides basic message operations.
/// </summary>
public interface IMessageSession
{
    /// <summary>
    /// Sends the provided message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="sendOptions">The options for the send.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7892",
        ReplacementTypeOrMember = "Send<T>(T, SendOptions, CancellationToken) or Send(object, Type, SendOptions, CancellationToken)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic or explicit Type overload instead.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the provided typed message.
    /// </summary>
    /// <typeparam name="T">The type of message, usually an interface.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="sendOptions">The options for the send.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    [OverloadResolutionPriority(-1)]
    Task Send<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        return Send(message!, typeof(T), sendOptions, cancellationToken);
    }

    /// <summary>
    /// Sends the provided message with the specified message type.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The declared message type.</param>
    /// <param name="sendOptions">The options for the send.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = MessageOperations.DefaultInterfaceTrimmingSuppressionJustification)]
    Task Send(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        MessageTypeValidator.Validate(message, messageType);
        return Send(message, sendOptions, cancellationToken);
    }

    /// <summary>
    /// Instantiates a message of type T and sends it.
    /// </summary>
    /// <typeparam name="T">The type of message, usually an interface.</typeparam>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    /// <param name="sendOptions">The options for the send.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    Task Send<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish the message to subscribers.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="publishOptions">The options for the publish.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7892",
        ReplacementTypeOrMember = "Publish<T>(T, PublishOptions, CancellationToken) or Publish(object, Type, PublishOptions, CancellationToken)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic or explicit Type overload instead.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the provided typed message.
    /// </summary>
    /// <typeparam name="T">The type of message, usually an interface.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="publishOptions">The options for the publish.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    [OverloadResolutionPriority(-1)]
    Task Publish<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        return Publish(message!, typeof(T), publishOptions, cancellationToken);
    }

    /// <summary>
    /// Publishes the provided message with the specified message type.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="messageType">The declared message type.</param>
    /// <param name="publishOptions">The options for the publish.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = MessageOperations.DefaultInterfaceTrimmingSuppressionJustification)]
    Task Publish(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        MessageTypeValidator.Validate(message, messageType);
        return Publish(message, publishOptions, cancellationToken);
    }

    /// <summary>
    /// Instantiates a message of type T and publishes it.
    /// </summary>
    /// <typeparam name="T">The type of message, usually an interface.</typeparam>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    /// <param name="publishOptions">Specific options for this event.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    Task Publish<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to receive published messages of the specified type.
    /// This method is only necessary if you turned off auto-subscribe.
    /// </summary>
    /// <param name="eventType">The type of event to subscribe to.</param>
    /// <param name="subscribeOptions">Options for the subscribe.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes to receive published messages of the specified type.
    /// </summary>
    /// <param name="eventType">The type of event to unsubscribe to.</param>
    /// <param name="unsubscribeOptions">Options for the subscribe.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default);
}