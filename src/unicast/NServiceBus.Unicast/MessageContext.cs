using System;
using System.Collections.Generic;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast
{
    /// <summary>
    /// Implementation of IMessageContext
    /// </summary>
    public class MessageContext : IMessageContext
    {
        private TransportMessage transportMessage;
        private HeaderAdapter headers;

        /// <summary>
        /// Initializes message context from the transport message.
        /// </summary>
        /// <param name="transportMessage"></param>
        public MessageContext(TransportMessage transportMessage)
        {
            this.transportMessage = transportMessage;
            headers = new HeaderAdapter(transportMessage.Headers);
        }

        #region IMessageContext Members

        IDictionary<string, string> IMessageContext.Headers
        {
            get { return headers; }
        }

        string IMessageContext.Id
        {
            get { return this.transportMessage.IdForCorrelation; }
        }

        string IMessageContext.ReturnAddress
        {
            get { return this.transportMessage.ReturnAddress; }
        }

        #endregion
    }
}
