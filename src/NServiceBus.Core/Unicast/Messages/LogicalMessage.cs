namespace NServiceBus.Unicast.Messages
{
    using System;

    /// <summary>
    /// The logical message.
    /// </summary>
    public class LogicalMessage
    {
        readonly LogicalMessageFactory factory;

        internal LogicalMessage(MessageMetadata metadata, object message,LogicalMessageFactory factory)
        {
            this.factory = factory;
            Instance = message;
            Metadata = metadata;
        }

        /// <summary>
        /// Updates the message instance.
        /// </summary>
        /// <param name="newInstance">The new instance.</param>
        public void UpdateMessageInstance(object newInstance)
        {
            Guard.AgainstNull(newInstance, "newInstance");
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
        /// Message metadata.
        /// </summary>
        public MessageMetadata Metadata { get; private set; }

        /// <summary>
        /// The message instance.
        /// </summary>
        public object Instance { get; private set; }
    }

    class ControlMessage : LogicalMessage
    {
        internal ControlMessage(string purpose) : base(null, null, null)
        {
            Purpose = purpose;
        }

        public string Purpose { get; private set; }
    }
}