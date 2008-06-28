using System;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.Subscriptions.Msmq
{
    /// <summary>
    /// Describes an entry in the list of subscriptions.
    /// </summary>
    public class Entry
    {
        /// <summary>
        /// Initializes a new Entry for the provided message type and
        /// subscription request.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="msg"></param>
        public Entry(Type messageType, TransportMessage msg)
        {
            this.msg = msg;
            this.messageType = messageType;
        }

        private Type messageType;

        /// <summary>
        /// Gets the message type for the subscription entry.
        /// </summary>
        public Type MessageType
        {
            get { return messageType; }
        }

        private TransportMessage msg;

        /// <summary>
        /// Gets the subscription request message.
        /// </summary>
        public TransportMessage Msg
        {
            get { return msg; }
        }
    }
}
