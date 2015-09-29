namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Implementation of IMessageContext.
    /// </summary>
    public class MessageContext : IMessageContext
    {
        TransportMessage transportMessage;

        /// <summary>
        /// Initializes message context from the transport message.
        /// </summary>
        public MessageContext(TransportMessage transportMessage)
        {
            this.transportMessage = transportMessage;
        }

        IDictionary<string, string> IMessageContext.Headers => transportMessage.Headers;

        /// <summary>
        /// The time at which the incoming message was sent.
        /// </summary>
        public DateTime TimeSent
        {
            get
            {
                string timeSent;
                if (transportMessage.Headers.TryGetValue(Headers.TimeSent, out timeSent))
                {
                    return DateTimeExtensions.ToUtcDateTime(timeSent);
                }

                return DateTime.MinValue;
            }
        }

        string IMessageContext.Id => transportMessage.Id;

        string IMessageContext.ReplyToAddress => transportMessage.ReplyToAddress;
    }
}
