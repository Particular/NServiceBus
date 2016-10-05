namespace NServiceBus.Pipeline
{
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
            Guard.AgainstNull(nameof(message), message);

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
            Guard.AgainstNull(nameof(messageType), messageType);
            Guard.AgainstNull(nameof(message), message);

            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }

            var realMessageType = messageMapper.GetMappedTypeFor(messageType);

            return new LogicalMessage(messageMetadataRegistry.GetMessageMetadata(realMessageType), message);
        }

        IMessageMapper messageMapper;
        MessageMetadataRegistry messageMetadataRegistry;
    }
}