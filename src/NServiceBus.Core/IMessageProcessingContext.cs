namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Particular.Obsoletes;

/// <summary>
/// The context of the currently processed message within the processing pipeline.
/// </summary>
public interface IMessageProcessingContext : IPipelineContext
{
    /// <summary>
    /// The Id of the currently processed message.
    /// </summary>
    string MessageId { get; }

    /// <summary>
    /// The address of the endpoint that sent the current message being handled.
    /// </summary>
    string ReplyToAddress { get; }

    /// <summary>
    /// Gets the list of key/value pairs found in the header of the message.
    /// </summary>
    IReadOnlyDictionary<string, string> MessageHeaders { get; }

    /// <summary>
    /// Sends the message to the endpoint which sent the message currently being handled.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="options">Options for this reply.</param>
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7906",
        ReplacementTypeOrMember = "Reply<T>(T, ReplyOptions) or Reply(object, Type, ReplyOptions)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic or explicit Type overload instead.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    Task Reply(object message, ReplyOptions options);

    /// <summary>
    /// Sends the typed message to the endpoint which sent the message currently being handled.
    /// </summary>
    /// <typeparam name="T">The type used to reply with the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="options">Options for this reply.</param>
    [OverloadResolutionPriority(-1)]
    Task Reply<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, ReplyOptions options)
    {
        return Reply(message!, typeof(T), options);
    }

    /// <summary>
    /// Sends the message to the endpoint which sent the message currently being handled with the specified message type. The declared type controls how the message is routed and the message type header recorded on the message.
    /// </summary>
    /// <param name="message">The message to send. Must be assignable to <paramref name="messageType" />.</param>
    /// <param name="messageType">The declared logical message type. It can differ from the runtime type of <paramref name="message" /> as long as the instance is assignable to it.</param>
    /// <param name="options">Options for this reply.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> or <paramref name="messageType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="message" /> is not assignable to <paramref name="messageType" />.</exception>
    /// <remarks>
    /// Third-party implementations that inherit this default implementation fall back to the object overload and route by the runtime type of <paramref name="message" />. Override this method to preserve a declared <paramref name="messageType" /> that differs from the runtime type.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = MessageOperations.DefaultInterfaceTrimmingSuppressionJustification)]
    Task Reply(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, ReplyOptions options)
    {
        MessageTypeValidator.Validate(message, messageType);
        return Reply(message, options);
    }

    /// <summary>
    /// Instantiates a message of type T and performs a regular Reply.
    /// </summary>
    /// <typeparam name="T">The type used to reply with the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="messageConstructor">An action which initializes properties of the message.</param>
    /// <param name="options">Options for this reply.</param>
    Task Reply<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(Action<T> messageConstructor, ReplyOptions options);

    /// <summary>
    /// Forwards the current message being handled to the destination maintaining
    /// all of its transport-level properties and headers.
    /// </summary>
    Task ForwardCurrentMessageTo(string destination);
}