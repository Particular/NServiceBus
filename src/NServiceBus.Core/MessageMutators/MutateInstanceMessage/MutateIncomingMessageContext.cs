#nullable enable

namespace NServiceBus.MessageMutator;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Particular.Obsoletes;

/// <summary>
/// Provides ways to mutate the outgoing message instance.
/// </summary>
public class MutateIncomingMessageContext : ICancellableContext
{
    /// <summary>
    /// Initializes the context.
    /// </summary>
    public MutateIncomingMessageContext(object message, Dictionary<string, string> headers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(message);
        Headers = headers;
        this.message = message;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// The current incoming message.
    /// </summary>
    public object Message
    {
        get => message;
        [ObsoleteMetadata(ReplacementTypeOrMember = "UpdateMessageInstance<T>(T)",
            TreatAsErrorFromVersion = "11",
            RemoveInVersion = "12")]
        [Obsolete("Use 'UpdateMessageInstance<T>(T)' or 'UpdateMessageInstance(object, Type)' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
        [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            MessageInstanceChanged = true;
            message = value;
        }
    }

    /// <summary>
    /// Replaces the current incoming message with the provided typed message instance.
    /// </summary>
    /// <typeparam name="T">The type used to update the message. It determines the logical message type and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="newMessage">The replacement message instance.</param>
    public void UpdateMessageInstance<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T newMessage) => UpdateMessageInstance(newMessage!, typeof(T));

    /// <summary>
    /// Replaces the current incoming message with the provided message instance and message type. The declared type determines the logical message type.
    /// </summary>
    /// <param name="newMessage">The replacement message instance. Must be assignable to <paramref name="messageType" />.</param>
    /// <param name="messageType">The declared logical message type. It can differ from the runtime type of <paramref name="newMessage" /> as long as the instance is assignable to it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="newMessage" /> or <paramref name="messageType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="newMessage" /> is not assignable to <paramref name="messageType" />.</exception>
    public void UpdateMessageInstance(object newMessage, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        MessageTypeValidator.Validate(newMessage, messageType);
        message = newMessage;
        MessageInstanceChanged = true;
        ReplacementMessageType = messageType;
    }

    /// <summary>
    /// The current incoming headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; }

    /// <summary>
    /// A <see cref="CancellationToken"/> to observe.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    object message;

    internal bool MessageInstanceChanged;

    [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)]
    internal Type? ReplacementMessageType;
}