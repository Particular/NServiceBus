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
        public Entry(Type messageType, TransportMessage msg)
        {
            this.Subscriber = msg.ReturnAddress;
            this.messageType = messageType.AssemblyQualifiedName;
            this.TypeOfMessage = messageType;
        }

        public Entry(string messageType, string subscriber)
        {
            this.Subscriber = subscriber;
            this.messageType = messageType;
            this.TypeOfMessage = Type.GetType(messageType);
        }

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


        public bool Matches(object message)
        {
            return Matches(message.GetType());
        }

        public bool Matches(Type msgType)
        {
            if (TypeOfMessage == null)
                return false;

            return TypeOfMessage.IsAssignableFrom(msgType);
        }
    }
}
