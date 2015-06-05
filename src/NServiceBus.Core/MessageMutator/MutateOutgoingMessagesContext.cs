namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides ways to mutate the outgoing message instance
    /// </summary>
    public class MutateOutgoingMessagesContext
    {
        /// <summary>
        /// The current message instance being sent
        /// </summary>
        public object MessageInstance
        {
            get
            {
                return instance;
            }
            set
            {
                MessageInstanceChanged = true;
                instance = value;

            }
        }

        /// <summary>
        /// Initializes the context
        /// </summary>
        public MutateOutgoingMessagesContext(object messageInstance)
        {
            Headers = new Dictionary<string, string>();
            MessageInstance = messageInstance;
        }


        /// <summary>
        /// Allows headers to be set
        /// </summary>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public void SetHeader(string key, string value)
        {
            Headers[key] = value;
        }

        internal readonly Dictionary<string, string> Headers;

        internal bool MessageInstanceChanged; 
        
        object instance;
    }
}