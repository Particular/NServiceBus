namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Implementation of IMessageContext
    /// </summary>
    public class MessageContext : IMessageContext
    {
        private readonly TransportMessage transportMessage;

        /// <summary>
        /// Initializes message context from the transport message.
        /// </summary>
        public MessageContext(TransportMessage transportMessage)
        {
            this.transportMessage = transportMessage;
        }

        IDictionary<string, string> IMessageContext.Headers
        {
            get { return transportMessage.Headers; }
        }

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

        string IMessageContext.Id
        {
            get { return transportMessage.Id; }
        }

        Address IMessageContext.ReplyToAddress
        {
            get { return transportMessage.ReplyToAddress; }
        }
    }
}
