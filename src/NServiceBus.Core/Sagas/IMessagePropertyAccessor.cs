#nullable enable
namespace NServiceBus;

using System;

/// <summary>
/// Accessor for a message property value.
/// </summary>
public abstract class MessagePropertyAccessor
{
    /// <summary>
    /// The type of the message.
    /// </summary>
    public abstract Type MessageType { get; }

    /// <summary>
    /// Returns the value of the property on the given message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The value of the property of the message.</returns>
    public abstract object? AccessFrom(object message);
}