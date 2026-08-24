namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Extensibility;
using Particular.Obsoletes;

/// <summary>
/// The context for the current message handling pipeline.
/// </summary>
public interface IPipelineContext : ICancellableContext, IExtendable
{
    /// <summary>
    /// Sends the provided message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="options">The options for the send.</param>
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7906",
        ReplacementTypeOrMember = "Send<T>(T, SendOptions) or Send(object, Type, SendOptions)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic or explicit Type overload instead.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    Task Send(object message, SendOptions options);

    /// <summary>
    /// Sends the provided typed message.
    /// </summary>
    /// <typeparam name="T">The type used to send the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="options">The options for the send.</param>
    [OverloadResolutionPriority(-1)]
    Task Send<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, SendOptions options)
    {
        return Send(message!, typeof(T), options);
    }

    /// <summary>
    /// Sends the provided message with the specified message type. The declared type controls how the message is routed and the message type header recorded on the message.
    /// </summary>
    /// <param name="message">The message to send. Must be assignable to <paramref name="messageType" />.</param>
    /// <param name="messageType">The declared logical message type. It can differ from the runtime type of <paramref name="message" /> as long as the instance is assignable to it.</param>
    /// <param name="options">The options for the send.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> or <paramref name="messageType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="message" /> is not assignable to <paramref name="messageType" />.</exception>
    /// <remarks>
    /// Third-party implementations that inherit this default implementation fall back to the object overload and route by the runtime type of <paramref name="message" />. Override this method to preserve a declared <paramref name="messageType" /> that differs from the runtime type.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = MessageOperations.DefaultInterfaceTrimmingSuppressionJustification)]
    Task Send(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, SendOptions options)
    {
        MessageTypeValidator.Validate(message, messageType);
        return Send(message, options);
    }

    /// <summary>
    /// Instantiates a message of type T and sends it.
    /// </summary>
    /// <typeparam name="T">The type used to send the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    /// <param name="options">The options for the send.</param>
    Task Send<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(Action<T> messageConstructor, SendOptions options);

    /// <summary>
    /// Publish the message to subscribers.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">The options for the publish.</param>
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7906",
        ReplacementTypeOrMember = "Publish<T>(T, PublishOptions) or Publish(object, Type, PublishOptions)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic or explicit Type overload instead.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    Task Publish(object message, PublishOptions options);

    /// <summary>
    /// Publishes the provided typed message.
    /// </summary>
    /// <typeparam name="T">The type used to publish the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">The options for the publish.</param>
    [OverloadResolutionPriority(-1)]
    Task Publish<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, PublishOptions options)
    {
        return Publish(message!, typeof(T), options);
    }

    /// <summary>
    /// Publishes the provided message with the specified message type. The declared type controls how the message is routed and the message type header recorded on the message.
    /// </summary>
    /// <param name="message">The message to publish. Must be assignable to <paramref name="messageType" />.</param>
    /// <param name="messageType">The declared logical message type. It can differ from the runtime type of <paramref name="message" /> as long as the instance is assignable to it.</param>
    /// <param name="options">The options for the publish.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> or <paramref name="messageType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="message" /> is not assignable to <paramref name="messageType" />.</exception>
    /// <remarks>
    /// Third-party implementations that inherit this default implementation fall back to the object overload and route by the runtime type of <paramref name="message" />. Override this method to preserve a declared <paramref name="messageType" /> that differs from the runtime type.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = MessageOperations.DefaultInterfaceTrimmingSuppressionJustification)]
    Task Publish(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, PublishOptions options)
    {
        MessageTypeValidator.Validate(message, messageType);
        return Publish(message, options);
    }

    /// <summary>
    /// Instantiates a message of type T and publishes it.
    /// </summary>
    /// <typeparam name="T">The type used to publish the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    /// <param name="publishOptions">Specific options for this event.</param>
    Task Publish<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(Action<T> messageConstructor, PublishOptions publishOptions);
}