namespace NServiceBus.Pipeline
{
    using System;
    using Unicast.Messages;

    /// <summary>
    /// The logical message.
    /// </summary>
    public class LogicalMessage
    {
        internal LogicalMessage(MessageMetadata metadata, object message, LogicalMessageFactory factory)
        {
            this.factory = factory;
            Instance = message;
            Metadata = metadata;
        }

        /// <summary>
        /// The <see cref="Type" /> of the message instance.
        /// </summary>
        public Type MessageType => Metadata.MessageType;


        /// <summary>
        /// Message metadata.
        /// </summary>
        public MessageMetadata Metadata { get; private set; }

        /// <summary>
        /// The message instance.
        /// </summary>
        public object Instance { get; private set; }

        /// <summary>
        /// Updates the message instance.
        /// </summary>
        /// <param name="newInstance">The new instance.</param>
        public void UpdateMessageInstance(object newInstance)
        {
            Guard.AgainstNull(nameof(newInstance), newInstance);
            var sameInstance = ReferenceEquals(Instance, newInstance);

            Instance = newInstance;

            if (sameInstance)
            {
                return;
            }

            var newLogicalMessage = factory.Create(newInstance);

            Metadata = newLogicalMessage.Metadata;
        }

        LogicalMessageFactory factory;
    }
}