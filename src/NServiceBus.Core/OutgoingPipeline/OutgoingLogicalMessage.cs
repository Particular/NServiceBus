namespace NServiceBus.OutgoingPipeline
{
    using System;

    /// <summary>
    /// Represents a logical message about to be push out to the transport.
    /// </summary>
    public class OutgoingLogicalMessage
    {

        /// <summary>
        /// Initializes the message with the given instance. Message type will be set to the instance type.
        /// </summary>
        public OutgoingLogicalMessage(object message)
        {
            Guard.AgainstNull(message, "message");

            MessageType = message.GetType();
            Instance = message;
        }

        /// <summary>
        /// Initializes the message with a explicit message type and instance. Use this constructor if the message type is different from the instance type.
        /// </summary>
        public OutgoingLogicalMessage(Type messageType, object message)
        {
            Guard.AgainstNull(messageType, "messageType");
            Guard.AgainstNull(message, "message");

            MessageType = messageType;
            Instance = message;
        }


        /// <summary>
        /// The <see cref="Type"/> of the message instance.
        /// </summary>
        public Type MessageType { get; private set; }

        /// <summary>
        /// The message instance.
        /// </summary>
        public object Instance { get; private set; }
    }
}