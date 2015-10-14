namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using NServiceBus.Transports;

    /// <summary>
    /// Implementation of IMessageContext.
    /// </summary>
    class MessageContext
    {
        IncomingMessage incomingMessage;

        /// <summary>
        /// Initializes message context from the transport message.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "MessageContext(IncomingMessage incomingMessage)")]
        public MessageContext(TransportMessage incomingMessage)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes message context from the incoming message.
        /// </summary>
        public MessageContext(IncomingMessage incomingMessage)
        {
            this.incomingMessage = incomingMessage;
            Headers = new ReadOnlyDictionary<string, string>(incomingMessage.Headers);
        }

        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>
        /// The time at which the incoming message was sent.
        /// </summary>
        public DateTime TimeSent
        {
            get
            {
                string timeSent;
                if (incomingMessage.Headers.TryGetValue(NServiceBus.Headers.TimeSent, out timeSent))
                {
                    return DateTimeExtensions.ToUtcDateTime(timeSent);
                }

                return DateTime.MinValue;
            }
        }

        public string Id => incomingMessage.MessageId;

        public string ReplyToAddress => incomingMessage.GetReplyToAddress();
    }
}
