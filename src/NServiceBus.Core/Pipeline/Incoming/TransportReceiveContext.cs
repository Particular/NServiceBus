﻿namespace NServiceBus.Pipeline.Contexts
{
    using Transports;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public class TransportReceiveContext : IncomingContext
    {
        internal TransportReceiveContext(IncomingMessage receivedMessage, BehaviorContext parentContext)
            : base(parentContext)
        {
            Guard.AgainstNull("receivedMessage", receivedMessage);
            Guard.AgainstNull("parentContext", parentContext);

            Set(receivedMessage);
            Message = receivedMessage;
        }

        /// <summary>
        /// The physical message beeing processed.
        /// </summary>
        public IncomingMessage Message { get; private set; }
    }
}