using System.Collections.Generic;

namespace NServiceBus.Unicast
{
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

        string IMessageContext.Id
        {
            get { return transportMessage.IdForCorrelation; }
        }

        Address IMessageContext.ReplyToAddress
        {
            get { return transportMessage.ReplyToAddress; }
        }
    }
}
