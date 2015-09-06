namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides ways to mutate the outgoing message instance.
    /// </summary>
    public class MutateIncomingMessageContext
    {
        object messageInstance;

        /// <summary>
        /// Initializes the context.
        /// </summary>
        public MutateIncomingMessageContext(object messageInstance, Dictionary<string, string> headers)
        {
            Guard.AgainstNull("headers", headers);
            Guard.AgainstNull("messageInstance", messageInstance);
            Headers = headers;
            this.messageInstance = messageInstance;
        }

        /// <summary>
        /// The current imcoming message.
        /// </summary>
        public object MessageInstance
        {
            get
            {
                return messageInstance;
            }
            set
            {
                Guard.AgainstNull("value", value); 
                MessageInstanceChanged = true;
                messageInstance = value;
            }
        }

        internal bool MessageInstanceChanged;

        /// <summary>
        /// The current incoming headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; private set; }
    }
}