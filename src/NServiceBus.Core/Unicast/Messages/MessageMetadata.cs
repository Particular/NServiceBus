namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Message metadata class.
    /// </summary>
    public partial class MessageMetadata
    {
        /// <summary>
        /// Create a new instance of <see cref="MessageMetadata"/>.
        /// </summary>
        /// <param name="messageType">The type of the message this metadata belongs to.</param>
        /// <param name="messageHierarchy">the hierarchy of all message types implemented by the message this metadata belongs to.</param>
        public MessageMetadata(Type messageType, IEnumerable<Type> messageHierarchy = null)
        {
            MessageType = messageType;
            MessageHierarchy = (messageHierarchy == null ? new List<Type>() : new List<Type>(messageHierarchy)).AsReadOnly();
        }

        /// <summary>
        /// The <see cref="Type" /> of the message instance.
        /// </summary>
        public Type MessageType { get; private set; }


        /// <summary>
        /// The message instance hierarchy.
        /// </summary>
        public IEnumerable<Type> MessageHierarchy { get; private set; }
    }
}