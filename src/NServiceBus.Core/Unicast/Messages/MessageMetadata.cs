namespace NServiceBus.Unicast.Messages
{
    using System;

    /// <summary>
    /// Message metadata class.
    /// </summary>
    public partial class MessageMetadata
    {
        static Type[] emptyHierarchy = new Type[0];

        /// <summary>
        /// Create a new instance of <see cref="MessageMetadata"/>.
        /// </summary>
        /// <param name="messageType">The type of the message this metadata belongs to.</param>
        public MessageMetadata(Type messageType) : this(messageType, null)
        {
        }

        /// <summary>
        /// Create a new instance of <see cref="MessageMetadata"/>.
        /// </summary>
        /// <param name="messageType">The type of the message this metadata belongs to.</param>
        /// <param name="messageHierarchy">the hierarchy of all message types implemented by the message this metadata belongs to.</param>
        public MessageMetadata(Type messageType, Type[] messageHierarchy)
        {
            MessageType = messageType;
            MessageHierarchy = messageHierarchy ?? emptyHierarchy;
        }

        /// <summary>
        /// The <see cref="Type" /> of the message instance.
        /// </summary>
        public Type MessageType { get; private set; }


        /// <summary>
        /// The message instance hierarchy.
        /// </summary>
        public Type[] MessageHierarchy { get; private set; }
    }
}