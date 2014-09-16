namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;


    /// <summary>
    /// The logical message.
    /// </summary>
    public class LogicalMessage
    {
        readonly LogicalMessageFactory factory;

        internal LogicalMessage(Dictionary<string, string> headers, LogicalMessageFactory factory)
        {
            this.factory = factory;
            Metadata = new MessageMetadata();
            Headers = headers;
        }

        internal LogicalMessage(MessageMetadata metadata, object message, Dictionary<string, string> headers, LogicalMessageFactory factory)
        {
            this.factory = factory;
            Instance = message;
            Metadata = metadata;
            Headers = headers;
        }

        /// <summary>
        /// Updates the message instance.
        /// </summary>
        /// <param name="newInstance">The new instance.</param>
        public void UpdateMessageInstance(object newInstance)
        {
            var sameInstance = ReferenceEquals(Instance, newInstance);
            
            Instance = newInstance;

            if (sameInstance)
            {
                return;
            }

            var newLogicalMessage = factory.Create(newInstance);

            Metadata = newLogicalMessage.Metadata;
        }

        /// <summary>
        /// The <see cref="Type"/> of the message instance.
        /// </summary>
        public Type MessageType
        {
            get
            {
                return Metadata.MessageType;
            }
        }

        /// <summary>
        ///     Gets other applicative out-of-band information.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// Message metadata.
        /// </summary>
        public MessageMetadata Metadata { get; private set; }

        /// <summary>
        /// The message instance.
        /// </summary>
        public object Instance { get; private set; }
    }
}