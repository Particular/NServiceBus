namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides ways to mutate the outgoing message instance.
    /// </summary>
    public class MutateIncomingMessageContext
    {
        object message;

        /// <summary>
        /// Initializes the context.
        /// </summary>
        public MutateIncomingMessageContext(object message, IDictionary<string, string> headers)
        {
            Guard.AgainstNull("headers", headers);
            Guard.AgainstNull("message", message);
            Headers = headers;
            this.message = message;
        }

        /// <summary>
        /// The current imcoming message.
        /// </summary>
        public object Message
        {
            get
            {
                return message;
            }
            set
            {
                Guard.AgainstNull("value", value);
                MessageChanged = true;
                message = value;
            }
        }

        internal bool MessageChanged;

        /// <summary>
        /// The current incoming headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; private set; }
    }
}