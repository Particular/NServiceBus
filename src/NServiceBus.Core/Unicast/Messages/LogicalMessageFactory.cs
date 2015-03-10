namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using MessageInterfaces;
    using Pipeline;


    /// <summary>
    /// Factory to create <see cref="LogicalMessage"/>s.
    /// </summary>
    public class LogicalMessageFactory
    {
        readonly MessageMetadataRegistry messageMetadataRegistry;
        readonly IMessageMapper messageMapper;
        
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="messageMetadataRegistry"></param>
        /// <param name="messageMapper"></param>
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
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

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
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (messageType == null)
            {
                throw new ArgumentNullException("messageType");
            }

            var realMessageType = messageMapper.GetMappedTypeFor(messageType);

            return new LogicalMessage(messageMetadataRegistry.GetMessageMetadata(realMessageType), message, this);
        }
    }
}