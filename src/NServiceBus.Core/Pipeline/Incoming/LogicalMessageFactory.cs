namespace NServiceBus.Unicast.Messages
{
    using System;
    using NServiceBus.MessageInterfaces;

    /// <summary>
    /// Factory to create <see cref="LogicalMessage"/>s.
    /// </summary>
    public class LogicalMessageFactory
    {
        MessageMetadataRegistry messageMetadataRegistry;
        IMessageMapper messageMapper;
        
        /// <summary>
        /// Initializes a new instance of <see cref="LogicalMessageFactory"/>.
        /// </summary>
        public LogicalMessageFactory(MessageMetadataRegistry messageMetadataRegistry, IMessageMapper messageMapper)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.messageMapper = messageMapper;
        }

        /// <summary>
        /// Creates a new <see cref="LogicalMessage"/> using the specified message instance.
        /// </summary>
        /// <param name="message">The message instance.</param>
        /// <returns>A new <see cref="LogicalMessage"/>.</returns>
        public LogicalMessage Create(object message)
        {
            Guard.AgainstNull("message", message);

            return Create(message.GetType(), message);
        }

        /// <summary>
        /// Creates a new <see cref="LogicalMessage"/> using the specified messageType, message instance and headers.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <param name="message">The message instance.</param>
        /// <returns>A new <see cref="LogicalMessage"/>.</returns>
        public LogicalMessage Create(Type messageType, object message)
        {
            Guard.AgainstNull("messageType", messageType);
            Guard.AgainstNull("message", message);

            if (messageType == null)
            {
                throw new ArgumentNullException("messageType");
            }

            var realMessageType = messageMapper.GetMappedTypeFor(messageType);

            return new LogicalMessage(messageMetadataRegistry.GetMessageMetadata(realMessageType), message, this);
        }
    }
}