namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using MessageInterfaces;
    using NServiceBus.Pipeline.Contexts;
    using Pipeline;


    /// <summary>
    /// Factory to create <see cref="LogicalMessage"/>s.
    /// </summary>
    public class LogicalMessageFactory
    {
        readonly MessageMetadataRegistry messageMetadataRegistry;
        readonly IMessageMapper messageMapper;
        readonly Func<BehaviorContext> contextGetter;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="messageMetadataRegistry"></param>
        /// <param name="messageMapper"></param>
        /// <param name="contextGetter"></param>
        public LogicalMessageFactory(MessageMetadataRegistry messageMetadataRegistry, IMessageMapper messageMapper, Func<BehaviorContext> contextGetter)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.messageMapper = messageMapper;
            this.contextGetter = contextGetter;
        }

        BehaviorContext CurrentContext { get { return contextGetter(); } }

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

            var headers = GetMessageHeaders(message);

            return Create(message.GetType(), message, headers);
        }

        /// <summary>
        /// Creates a new <see cref="LogicalMessage"/> using the specified messageType, message instance and headers.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <param name="message">The message instance.</param>
        /// <param name="headers">The message headers.</param>
        /// <returns>A new <see cref="LogicalMessage"/>.</returns>
        public LogicalMessage Create(Type messageType, object message, Dictionary<string, string> headers)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (messageType == null)
            {
                throw new ArgumentNullException("messageType");
            }

            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            var realMessageType = messageMapper.GetMappedTypeFor(messageType);

            return new LogicalMessage(messageMetadataRegistry.GetMessageMetadata(realMessageType), message, headers, this);
        }

        /// <summary>
        /// Creates a new control <see cref="LogicalMessage"/>.
        /// </summary>
        /// <param name="headers">Any additional headers</param>
        public LogicalMessage CreateControl(Dictionary<string, string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            headers.Add(Headers.ControlMessageHeader, true.ToString());

            return new LogicalMessage(headers, this);
        }

        Dictionary<string, string> GetMessageHeaders(object message)
        {
            OutgoingHeaders existingHeaders;
            if (!CurrentContext.TryGet(out existingHeaders))
            {
                return new Dictionary<string, string>();
            }
            return existingHeaders.GetAndRemoveAll(message);
        }
    }
}