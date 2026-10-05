#nullable enable

namespace NServiceBus.Pipeline;

using System;
using MessageInterfaces;
using Unicast.Messages;

/// <summary>
/// Factory to create <see cref="LogicalMessage" />s.
/// </summary>
public class LogicalMessageFactory
{
    /// <summary>
    /// Initializes a new instance of <see cref="LogicalMessageFactory" />.
    /// </summary>
    public LogicalMessageFactory(MessageMetadataRegistry messageMetadataRegistry, IMessageMapper messageMapper)
    {
        this.messageMetadataRegistry = messageMetadataRegistry;
        this.messageMapper = messageMapper;
    }

    /// <summary>
    /// Creates a new <see cref="LogicalMessage" /> using the specified message instance.
    /// </summary>
    /// <param name="message">The message instance.</param>
    /// <returns>A new <see cref="LogicalMessage" />.</returns>
    public LogicalMessage Create(object message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return Create(message.GetType(), message);
    }

    /// <summary>
    /// Creates a new <see cref="LogicalMessage" /> using the specified messageType, message instance and headers.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="message">The message instance.</param>
    /// <returns>A new <see cref="LogicalMessage" />.</returns>
    public LogicalMessage Create(Type messageType, object message)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(message);

        var realMessageType = messageMapper.GetMappedTypeFor(messageType);

        return Create(messageMetadataRegistry.GetMessageMetadata(realMessageType), message);
    }

    /// <summary>
    /// Creates a new <see cref="LogicalMessage" /> using the specified metadata and message instance without invoking the message mapper.
    /// </summary>
    /// <param name="metadata">The metadata for the message.</param>
    /// <param name="message">The message instance.</param>
    /// <returns>A new <see cref="LogicalMessage" />.</returns>
#pragma warning disable CA1822 // Mark members as static
    public LogicalMessage Create(MessageMetadata metadata, object message)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(message);

        return new LogicalMessage(metadata, message);
    }
#pragma warning restore CA1822 // Mark members as static

    readonly IMessageMapper messageMapper;
    readonly MessageMetadataRegistry messageMetadataRegistry;
}