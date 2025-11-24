#nullable enable
namespace NServiceBus;

using System;

/// <summary>
/// Strongly typed accessor for a message property.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public abstract class MessagePropertyAccessor<TMessage> : MessagePropertyAccessor
{
    /// <summary>
    /// Access the property from the given message.
    /// </summary>
    /// <param name="message">The given message instance.</param>
    /// <returns>The value of the property.</returns>
    protected abstract object? AccessFrom(TMessage message);

    /// <inheritdoc/>
    public sealed override Type MessageType { get; } = typeof(TMessage);

    /// <inheritdoc/>
    public sealed override object? AccessFrom(object message) => AccessFrom((TMessage)message);
}