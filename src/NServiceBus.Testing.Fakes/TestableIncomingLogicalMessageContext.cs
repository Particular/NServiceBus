namespace NServiceBus.Testing;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Pipeline;
using Unicast.Messages;

/// <summary>
/// A testable implementation of <see cref="IIncomingLogicalMessageContext" />.
/// </summary>
public partial class TestableIncomingLogicalMessageContext : TestableIncomingContext, IIncomingLogicalMessageContext
{
    /// <summary>
    /// Creates a new instance of <see cref="TestableIncomingLogicalMessageContext" />.
    /// </summary>
    public TestableIncomingLogicalMessageContext(IMessageCreator messageCreator = null) : base(messageCreator)
    {
    }

    /// <summary>
    /// Message being handled.
    /// </summary>
    public LogicalMessage Message { get; set; } = new LogicalMessage(new MessageMetadata(typeof(object)), new object());

    /// <summary>
    /// Headers for the incoming message.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = [];

    /// <summary>
    /// Tells if the message has been handled.
    /// </summary>
    public bool MessageHandled { get; set; }

    /// <summary>
    /// Updates the message instance contained in <see cref="LogicalMessage" />.
    /// </summary>
    /// <param name="newInstance">The new instance.</param>
    [RequiresUnreferencedCode(DynamicMemberTypeAccess.RuntimeTypeRoutingTrimmingMessage)]
    public virtual void UpdateMessageInstance(object newInstance)
    {
        Message = new LogicalMessage(new MessageMetadata(newInstance.GetType()), newInstance);
    }

    /// <summary>
    /// Updates the message instance contained in <see cref="LogicalMessage" /> while preserving the specified message type.
    /// </summary>
    /// <typeparam name="T">The type used to update the message. It determines the logical message type and can differ from the runtime type of the message instance as long as the instance is assignable to T.</typeparam>
    /// <param name="newInstance">The replacement message instance.</param>
    [OverloadResolutionPriority(-1)]
    public virtual void UpdateMessageInstance<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T newInstance)
    {
        UpdateMessageInstance(newInstance!, typeof(T));
    }

    /// <summary>
    /// Updates the message instance contained in <see cref="LogicalMessage" /> with the specified message type. The declared type determines the logical message type.
    /// </summary>
    /// <param name="newInstance">The replacement message instance. Must be assignable to <paramref name="messageType" />.</param>
    /// <param name="messageType">The declared logical message type. It can differ from the runtime type of <paramref name="newInstance" /> as long as the instance is assignable to it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="newInstance" /> or <paramref name="messageType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="newInstance" /> is not assignable to <paramref name="messageType" />.</exception>
    public virtual void UpdateMessageInstance(object newInstance, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType)
    {
        MessageTypeValidator.Validate(newInstance, messageType);
        Message = new LogicalMessage(new MessageMetadata(messageType), newInstance);
    }
}