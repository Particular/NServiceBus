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
            this.subscriber = msg.ReturnAddress;
            this.messageType = messageType.AssemblyQualifiedName;
            this.TypeOfMessage = messageType;
        }

        public Entry(string messageType, string subscriber)
        {
            this.subscriber = subscriber;
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

        private string subscriber;

        /// <summary>
        /// Gets the subscription request message.
        /// </summary>
        public string Subscriber
        {
            get { return subscriber; }
            set { subscriber = value; }
        }

        private Type TypeOfMessage;


        public bool Matches(object message)
        {
            return Matches(message.GetType());
        }

        public bool Matches(Type msgType)
        {
            return TypeOfMessage.IsAssignableFrom(msgType);
        }
    }
}
