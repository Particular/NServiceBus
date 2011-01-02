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
            get { return string.IsNullOrEmpty(transportMessage.IdForCorrelation) ? transportMessage.Id : transportMessage.IdForCorrelation; }
        }

        string IMessageContext.ReturnAddress
        {
            get { return transportMessage.ReturnAddress; }
        }

        DateTime IMessageContext.TimeSent
        {
            get { return transportMessage.TimeSent;  }
        }
    }
}
