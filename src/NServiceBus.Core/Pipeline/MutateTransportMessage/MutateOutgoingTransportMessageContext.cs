namespace NServiceBus.MessageMutator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using JetBrains.Annotations;

    /// <summary>
    /// Context class for <see cref="IMutateOutgoingTransportMessages"/>.
    /// </summary>
    public class MutateOutgoingTransportMessageContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MutateOutgoingTransportMessageContext"/>.
        /// </summary>
        public MutateOutgoingTransportMessageContext(object outgoingMessage, IReadOnlyDictionary<string, string> outgoingHeaders, object incomingMessage, IReadOnlyDictionary<string, string> incomingHeaders)
        {
            Guard.AgainstNull("outgoingHeaders", outgoingHeaders);
            Guard.AgainstNull("outgoingMessage", outgoingMessage);

            OutgoingHeaders = outgoingHeaders;

            Decorators = new List<Func<Stream, Stream>>();
            ModifiedHeaders = new Dictionary<string, string>();
            this.incomingHeaders = incomingHeaders;
            this.incomingMessage = incomingMessage;
        }

        /// <summary>
        /// The current outgoing headers.
        /// </summary>
        public IReadOnlyDictionary<string, string> OutgoingHeaders { get; private set; }

        /// <summary>
        /// The current outgoing message.
        /// </summary>
        public object OutgoingMessage { get; private set; }

        /// <summary>
        /// Gets the incoming message that initiated the current send if it exists.
        /// </summary>
        public bool TryGetIncomingMessage(out object message)
        {
            message = incomingMessage;
            return message != null;
        }

        /// <summary>
        /// Gets the incoming headers that initiated the current send if it exists.
        /// </summary>
        public bool TryGetIncomingHeaders(out IReadOnlyDictionary<string, string> incomingHeaders)
        {
            incomingHeaders = this.incomingHeaders;
            return incomingHeaders != null;
        }

        /// <summary>
        /// Registers a stream decorator.
        /// </summary>
        public void RegisterStreamMutation(Func<Stream, Stream> func)
        {
            Decorators.Add(func);
        }

        /// <summary>
        /// Sets a header on the outgoing message
        /// </summary>
        /// <param name="headersetbymutator"></param>
        /// <param name="someValue"></param>
        public void SetHeader(string headersetbymutator, string someValue)
        {
            ModifiedHeaders[headersetbymutator] = someValue;
        }

        internal IDictionary<string, string> ModifiedHeaders;
        internal List<Func<Stream, Stream>> Decorators;

        IReadOnlyDictionary<string, string> incomingHeaders;
        object incomingMessage;
    }
}