using System;
using Common.Logging;

namespace NServiceBus.Unicast.Subscriptions.Msmq
{
    /// <summary>
    /// Describes an entry in the list of subscriptions.
    /// </summary>
    [Serializable]
    public class Entry
    {
        /// <summary>
        /// Creates a new entry storing the message type and the subscriber.
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="messageType"></param>
        public Entry(string subscriber, string messageType)
        {
            Subscriber = subscriber;
            this.messageType = messageType;
            typeOfMessage = Type.GetType(messageType, false);

            if (typeOfMessage == null)
            {
                string warning = "Could not handle subscription for message type: " + messageType +
                            " from endpoint " + subscriber + ". Type not available on this endpoint.";

                Logger.Warn(warning);

                if (Logger.IsDebugEnabled)
                    throw new InvalidOperationException(warning);
            }
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
                typeOfMessage = Type.GetType(value);
            }
        }

        /// <summary>
        /// Gets the subscription request message.
        /// </summary>
        public string Subscriber { get; set; }

        private Type typeOfMessage;

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
            return typeOfMessage != null && typeOfMessage.IsAssignableFrom(msgType);
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (ISubscriptionStorage));
    }
}
