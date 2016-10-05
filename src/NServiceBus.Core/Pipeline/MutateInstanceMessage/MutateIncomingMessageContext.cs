namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides ways to mutate the outgoing message instance.
    /// </summary>
    public class MutateIncomingMessageContext
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        public MutateIncomingMessageContext(object message, Dictionary<string, string> headers)
        {
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(message), message);
            Headers = headers;
            this.message = message;
        }

        /// <summary>
        /// The current incoming message.
        /// </summary>
        public object Message
        {
            get { return message; }
            set
            {
                Guard.AgainstNull(nameof(value), value);
                MessageInstanceChanged = true;
                message = value;
            }
        }

        /// <summary>
        /// The current incoming headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        object message;

        internal bool MessageInstanceChanged;
    }
}