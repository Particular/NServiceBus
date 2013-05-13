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
        /// <param name="transportMessage"></param>
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
                if (transportMessage.Headers.ContainsKey(Headers.TimeSent))
                {
                    return DateTimeExtensions.ToUtcDateTime(transportMessage.Headers[Headers.TimeSent]);
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
