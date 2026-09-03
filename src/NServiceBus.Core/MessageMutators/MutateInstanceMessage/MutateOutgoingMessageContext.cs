#nullable enable

namespace NServiceBus.MessageMutator;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

/// <summary>
/// Provides ways to mutate the outgoing message instance.
/// </summary>
public class MutateOutgoingMessageContext : ICancellableContext
{
    /// <summary>
    /// Initializes the context.
    /// </summary>
    public MutateOutgoingMessageContext(object outgoingMessage, Dictionary<string, string> outgoingHeaders, object? incomingMessage, IReadOnlyDictionary<string, string>? incomingHeaders, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outgoingHeaders);
        ArgumentNullException.ThrowIfNull(outgoingMessage);
        OutgoingHeaders = outgoingHeaders;
        this.incomingMessage = incomingMessage;
        this.incomingHeaders = incomingHeaders;
        this.outgoingMessage = outgoingMessage;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// The current outgoing message.
    /// </summary>
    public object OutgoingMessage
    {
        get => outgoingMessage;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            MessageInstanceChanged = true;
            outgoingMessage = value;
        }
    }

    /// <summary>
    /// Replaces the current outgoing message with the provided typed message instance.
    /// </summary>
    /// <typeparam name="T">The type used to update the message. It determines how the message is routed and the message type header recorded on the message, and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="newMessage">The replacement message instance.</param>
    public void UpdateMessage<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T newMessage)
    {
        UpdateMessage(newMessage!, typeof(T));
    }

    /// <summary>
    /// Replaces the current outgoing message with the provided message instance and message type. The declared type controls how the message is routed and the message type header recorded on the message.
    /// </summary>
    /// <param name="newMessage">The replacement message instance. Must be assignable to <paramref name="messageType" />.</param>
    /// <param name="messageType">The declared logical message type. It can differ from the runtime type of <paramref name="newMessage" /> as long as the instance is assignable to it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="newMessage" /> or <paramref name="messageType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="newMessage" /> is not assignable to <paramref name="messageType" />.</exception>
    public void UpdateMessage(object newMessage, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        MessageTypeValidator.Validate(newMessage, messageType);
        OutgoingMessage = newMessage;
        ReplacementMessageType = messageType;
    }

    /// <summary>
    /// The current outgoing headers.
    /// </summary>
    public Dictionary<string, string> OutgoingHeaders { get; }

    /// <summary>
    /// A <see cref="CancellationToken"/> to observe.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the incoming message that initiated the current send if it exists.
    /// </summary>
    public bool TryGetIncomingMessage([NotNullWhen(true)] out object? incomingMessage)
    {
        incomingMessage = this.incomingMessage;
        return incomingMessage != null;
    }

    /// <summary>
    /// Gets the incoming headers that initiated the current send if it exists.
    /// </summary>
    public bool TryGetIncomingHeaders([NotNullWhen(true)] out IReadOnlyDictionary<string, string>? incomingHeaders)
    {
        incomingHeaders = this.incomingHeaders;
        return incomingHeaders != null;
    }

    readonly IReadOnlyDictionary<string, string>? incomingHeaders;
    readonly object? incomingMessage;

    internal bool MessageInstanceChanged;

    /// <summary>
    /// The declared logical message type of <see cref="OutgoingMessage" /> when a mutator supplied an explicit type, otherwise <see langword="null" />.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)]
    internal Type? ReplacementMessageType { get; set; }

    object outgoingMessage;
}