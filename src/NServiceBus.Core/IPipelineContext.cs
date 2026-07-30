namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Extensibility;

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
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    Task Send(object message, SendOptions options);

    /// <summary>
    /// Sends the provided typed message.
    /// </summary>
    /// <typeparam name="T">The type of message, usually an interface.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="options">The options for the send.</param>
    [OverloadResolutionPriority(-1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = MessageOperations.DefaultInterfaceTrimmingSuppressionJustification)]
    Task Send<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, SendOptions options)
    {
        return Send(message!, options);
    }

    /// <summary>
    /// Instantiates a message of type T and sends it.
    /// </summary>
    /// <typeparam name="T">The type of message, usually an interface.</typeparam>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    /// <param name="options">The options for the send.</param>
    Task Send<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(Action<T> messageConstructor, SendOptions options);

    /// <summary>
    /// Publish the message to subscribers.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">The options for the publish.</param>
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    Task Publish(object message, PublishOptions options);

    /// <summary>
    /// Publishes the provided typed message.
    /// </summary>
    /// <typeparam name="T">The type of message, usually an interface.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">The options for the publish.</param>
    [OverloadResolutionPriority(-1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = MessageOperations.DefaultInterfaceTrimmingSuppressionJustification)]
    Task Publish<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, PublishOptions options)
    {
        return Publish(message!, options);
    }

    /// <summary>
    /// Instantiates a message of type T and publishes it.
    /// </summary>
    /// <typeparam name="T">The type of message, usually an interface.</typeparam>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    /// <param name="publishOptions">Specific options for this event.</param>
    Task Publish<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(Action<T> messageConstructor, PublishOptions publishOptions);
}