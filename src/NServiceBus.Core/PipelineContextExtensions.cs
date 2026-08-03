#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

/// <summary>
/// Syntactic sugar for <see cref="IPipelineContext" />.
/// </summary>
public static class PipelineContextExtensions
{
    /// <summary>
    /// Sends the provided message.
    /// </summary>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="message">The message to send.</param>
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    public static Task Send(this IPipelineContext context, object message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        return context.Send(message, new SendOptions());
    }

    /// <summary>
    /// Sends the provided typed message.
    /// </summary>
    /// <typeparam name="T">The type used to send the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="message">The message to send.</param>
    [OverloadResolutionPriority(-1)]
    public static Task Send<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(this IPipelineContext context, T message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        return context.Send<T>(message, new SendOptions());
    }

    /// <summary>
    /// Sends the provided message with the specified message type.
    /// </summary>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The declared message type.</param>
    public static Task Send(this IPipelineContext context, object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        return context.Send(message, messageType, new SendOptions());
    }

    /// <summary>
    /// Instantiates a message of <typeparamref name="T" /> and sends it.
    /// </summary>
    /// <typeparam name="T">The type used to send the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    /// <remarks>
    /// The message will be sent to the destination configured for <typeparamref name="T" />.
    /// </remarks>
    public static Task Send<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(this IPipelineContext context, Action<T> messageConstructor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageConstructor);

        return context.Send(messageConstructor, new SendOptions());
    }

    /// <summary>
    /// Sends the message.
    /// </summary>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="destination">The address of the destination to which the message will be sent.</param>
    /// <param name="message">The message to send.</param>
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    public static Task Send(this IPipelineContext context, string destination, object message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(message);

        var options = new SendOptions();

        options.SetDestination(destination);

        return context.Send(message, options);
    }

    /// <summary>
    /// Sends the typed message to the given destination.
    /// </summary>
    /// <typeparam name="T">The type used to send the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="destination">The destination to which the message will be sent.</param>
    /// <param name="message">The message to send.</param>
    [OverloadResolutionPriority(-1)]
    public static Task Send<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(this IPipelineContext context, string destination, T message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(message);

        var options = new SendOptions();

        options.SetDestination(destination);

        return context.Send<T>(message, options);
    }

    /// <summary>
    /// Sends the message with the specified message type to the given destination.
    /// </summary>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="destination">The destination to which the message will be sent.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The declared message type.</param>
    public static Task Send(this IPipelineContext context, string destination, object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        var options = new SendOptions();

        options.SetDestination(destination);

        return context.Send(message, messageType, options);
    }

    /// <summary>
    /// Instantiates a message of type T and sends it to the given destination.
    /// </summary>
    /// <typeparam name="T">The type used to send the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="destination">The destination to which the message will be sent.</param>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    public static Task Send<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(this IPipelineContext context, string destination, Action<T> messageConstructor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(messageConstructor);

        var options = new SendOptions();

        options.SetDestination(destination);

        return context.Send(messageConstructor, options);
    }

    /// <summary>
    /// Sends the message back to the current endpoint.
    /// </summary>
    /// <param name="context">Object being extended.</param>
    /// <param name="message">The message to send.</param>
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    public static Task SendLocal(this IPipelineContext context, object message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        var options = new SendOptions();

        options.RouteToThisEndpoint();

        return context.Send(message, options);
    }

    /// <summary>
    /// Sends the typed message back to the current endpoint.
    /// </summary>
    /// <typeparam name="T">The type used to send the message locally. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="context">Object being extended.</param>
    /// <param name="message">The message to send.</param>
    [OverloadResolutionPriority(-1)]
    public static Task SendLocal<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(this IPipelineContext context, T message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        var options = new SendOptions();

        options.RouteToThisEndpoint();

        return context.Send<T>(message, options);
    }

    /// <summary>
    /// Sends the message with the specified message type back to the current endpoint.
    /// </summary>
    /// <param name="context">Object being extended.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The declared message type.</param>
    public static Task SendLocal(this IPipelineContext context, object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        var options = new SendOptions();

        options.RouteToThisEndpoint();

        return context.Send(message, messageType, options);
    }

    /// <summary>
    /// Instantiates a message of type T and sends it back to the current endpoint.
    /// </summary>
    /// <typeparam name="T">The type used to send the message locally. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="context">Object being extended.</param>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    public static Task SendLocal<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(this IPipelineContext context, Action<T> messageConstructor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageConstructor);

        var options = new SendOptions();

        options.RouteToThisEndpoint();

        return context.Send(messageConstructor, options);
    }

    /// <summary>
    /// Publish the message to subscribers.
    /// </summary>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="message">The message to publish.</param>
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    public static Task Publish(this IPipelineContext context, object message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        return context.Publish(message, new PublishOptions());
    }

    /// <summary>
    /// Publishes the provided typed message.
    /// </summary>
    /// <typeparam name="T">The type used to publish the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="message">The message to publish.</param>
    [OverloadResolutionPriority(-1)]
    public static Task Publish<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(this IPipelineContext context, T message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        return context.Publish<T>(message, new PublishOptions());
    }

    /// <summary>
    /// Publishes the provided message with the specified message type.
    /// </summary>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="messageType">The declared message type.</param>
    public static Task Publish(this IPipelineContext context, object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        return context.Publish(message, messageType, new PublishOptions());
    }

    /// <summary>
    /// Publish the message to subscribers.
    /// </summary>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <typeparam name="T">The message type.</typeparam>
    public static Task Publish<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(this IPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Publish<T>(_ => { }, new PublishOptions());
    }

    /// <summary>
    /// Instantiates a message of type T and publishes it.
    /// </summary>
    /// <typeparam name="T">The type used to publish the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    public static Task Publish<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(this IPipelineContext context, Action<T> messageConstructor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageConstructor);

        return context.Publish(messageConstructor, new PublishOptions());
    }
}