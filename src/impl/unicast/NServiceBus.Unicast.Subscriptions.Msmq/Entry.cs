using System;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.Subscriptions.Msmq
{
    /// <summary>
    /// Describes an entry in the list of subscriptions.
    /// </summary>
    [Serializable]
    public class Entry
    {
        /// <summary>
        /// Creates a new Entry storing the message type and subscriber (return address of the given message).
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="msg"></param>
        public Entry(Type messageType, TransportMessage msg)
        {
            this.Subscriber = msg.ReturnAddress;
            this.messageType = messageType.AssemblyQualifiedName;
            this.TypeOfMessage = messageType;
        }

        /// <summary>
        /// Creates a new entry storing the message type and the subscriber.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="subscriber"></param>
        public Entry(string messageType, string subscriber)
        {
            this.Subscriber = subscriber;
            this.messageType = messageType;
            this.TypeOfMessage = Type.GetType(messageType);
        }

        /// <summary>
        /// Empty constructor for serialization purposes.
        /// </summary>
        public Entry()
        {
        }

        private string messageType;

        /// <summary>
        /// Gets the message type for the subscription entry.
        /// </summary>
        public string MessageType
        {
            get { return messageType; }
            set
            {
                messageType = value;
                TypeOfMessage = Type.GetType(value);
            }
        }

        /// <summary>
        /// Gets the subscription request message.
        /// </summary>
        public string Subscriber { get; set; }

        private Type TypeOfMessage;

        /// <summary>
        /// Returns true if the given message is compatible with the entry's message type.
        /// Otherwise false.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool Matches(object message)
        {
            return Matches(message.GetType());
        }

        /// <summary>
        /// Returns true if the given message type is compatible with that of the entry.
        /// Otherwise false.
        /// </summary>
        /// <param name="msgType"></param>
        /// <returns></returns>
        public bool Matches(Type msgType)
        {
            if (TypeOfMessage == null)
                return false;

            return TypeOfMessage.IsAssignableFrom(msgType);
        }
    }
}
