namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;
    using System.Threading;
    using Transport;

    /// <summary>
    /// Context class for <see cref="IMutateIncomingTransportMessages" />.
    /// </summary>
    public class MutateIncomingTransportMessageContext : ICancellableContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MutateOutgoingTransportMessageContext" />.
        /// </summary>
        public MutateIncomingTransportMessageContext(MessageBody body, Dictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(body), body);
            Headers = headers;

            Body = body;

            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// The body of the message.
        /// </summary>
        public MessageBody Body { get; private set; }

        /// <summary>
        /// The current incoming headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// A <see cref="CancellationToken"/> to observe.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Replace the message body
        /// </summary>
        /// <param name="messageBody"></param>
        public void UpdateMessage(byte[] messageBody)
        {
            MessageBodyChanged = true;
            Body = new MessageBody(messageBody);
        }

        internal bool MessageBodyChanged;
    }
}